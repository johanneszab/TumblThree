using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models;
using TumblThree.Applications.DataModels;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using TumblThree.Domain;

namespace TumblThree.Applications.Downloader
{
    public class TumblrDownloader : CommonDownloader
    {

        private readonly IShellService shellService;
        private readonly ICrawlerService crawlerService;
        private readonly ISelectionService selectionService;
        private readonly TumblrBlog blog;

        public TumblrDownloader(IShellService shellService, ICrawlerService crawlerService, ISelectionService selectionService, TumblrBlog blog) : base(shellService)
        {
            this.shellService = shellService;
            this.crawlerService = crawlerService;
            this.selectionService = selectionService;
            this.blog = blog;
        }


        public async Task<bool> SetUpBlog()
        {
            blog.Name = ExtractSubDomain(blog.Url);
            blog.Url = ExtractUrl(blog.Url);
            blog.Type = Blog.BlogTypes.tumblr;
            await UpdateMetaInformation();
            blog.Online = await IsBlogOnline(blog.Url);
            CreateDataFolder("Index", blog.Location);
            CreateDataFolder(blog.Name, blog.Location);
            return true;
        }


        protected new XDocument RequestData(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                if (!String.IsNullOrEmpty(shellService.Settings.ProxyHost))
                {
                    request.Proxy = new WebProxy(shellService.Settings.ProxyHost, Int32.Parse(shellService.Settings.ProxyPort));
                }
                else
                {
                    request.Proxy = null;
                }
                request.KeepAlive = true;
                request.AllowAutoRedirect = true;
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.Pipelined = true;
                request.Timeout = shellService.Settings.TimeOut * 1000;
                request.ServicePoint.Expect100Continue = false;
                ServicePointManager.DefaultConnectionLimit = 400;
                //request.ContentLength = 0;
                //request.ContentType = "x-www-from-urlencoded";

                int bandwidth = 2000000;
                if (shellService.Settings.LimitScanBandwidth)
                {
                    bandwidth = shellService.Settings.Bandwidth;
                }

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (ThrottledStream stream = new ThrottledStream(response.GetResponseStream(), (bandwidth / shellService.Settings.ParallelImages) * 1024))
                    {
                        using (BufferedStream buffer = new BufferedStream(stream))
                        {
                            using (StreamReader reader = new StreamReader(buffer))
                            {
                                return XDocument.Load(reader);
                            }
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }


        protected override string ExtractUrl(string url)
        {
            return ("https://" + ExtractSubDomain(url) + ".tumblr.com/");
        }


        private string GetApiUrl(string url, int count, int start = 0)
        {
            /// <summary>
            /// construct the tumblr api post url of a blog.
            /// <para>the blog for the url</para>
            /// </summary>
            /// <returns>A string containing the api url of the blog.</returns>
            if (url.Last<char>() != '/')
                url += "/api/read";
            else
                url += "api/read";

            var parameters = new Dictionary<string, string>
            {
              { "num", count.ToString() }
            };
            if (start > 0)
                parameters["start"] = start.ToString();
            return url + "?" + UrlEncode(parameters);
        }


        private new Task<bool> IsBlogOnline(string url)
        {
            return Task<bool>.Factory.StartNew(() =>
            {
                url = GetApiUrl(url, 1);

                XDocument blogDoc = null;

                if (shellService.Settings.LimitConnections)
                {
                    crawlerService.Timeconstraint.Acquire();
                    blogDoc = RequestData(url);
                }
                else
                    blogDoc = RequestData(url);

                if (blogDoc != null)
                    return true;
                else
                    return false;
            },
            TaskCreationOptions.LongRunning);
        }


        private Task<TumblrBlog> UpdateMetaInformation()
        {
            return Task<TumblrBlog>.Factory.StartNew(() =>
            {
                string url = GetApiUrl(blog.Url, 1);

                XDocument blogDoc = null;

                if (shellService.Settings.LimitConnections)
                {
                    crawlerService.Timeconstraint.Acquire();
                    blogDoc = RequestData(url);
                }
                else
                    blogDoc = RequestData(url);

                if (blogDoc != null)
                {
                    blog.Title = blogDoc.Element("tumblr").Element("tumblelog").Attribute("title")?.Value;
                    blog.Description = blogDoc.Element("tumblr").Element("tumblelog")?.Value;
                    blog.TotalCount = Int32.Parse(blogDoc.Element("tumblr").Element("posts").Attribute("total")?.Value);
                    return blog;
                }
                else
                    return blog;
            },
            TaskCreationOptions.LongRunning);
        }


        private string resizeTumblrImageUrl(string imageUrl)
        {
            StringBuilder sb = new StringBuilder(imageUrl);
            return sb
                .Replace("_1280", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_540", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_500", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_400", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_250", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_100", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_75sq", "_" + shellService.Settings.ImageSize.ToString())
                .ToString();
        }



        public void CrawlTumblrBlog(IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            Logger.Verbose("ManagerController.CrawlCoreTumblrBlog:Start");

            BlockingCollection<Tuple<string, string, string>> bCollection = new BlockingCollection<Tuple<string, string, string>>();

            var producer = Task.Run(() => GetUrls(bCollection, progress, ct, pt));
            var consumer = Task.Run(() => DownloadTumblrBlog(bCollection, progress, ct, pt));
            var blogContent = producer.Result;
            bool limitHit = blogContent.Item2;
            var newImageUrls = blogContent.Item3;

            var newProgress = new DownloadProgress();
            newProgress.Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressUniqueDownloads);
            progress.Report(newProgress);

            // determine duplicates
            int duplicatePhotos = newImageUrls.Where(url => url.Item2.Equals("Photo"))
                .GroupBy(url => url.Item1)
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count() - 1);
            int duplicateVideos = newImageUrls.Where(url => url.Item2.Equals("Video"))
                .GroupBy(url => url.Item1)
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count() - 1);
            int duplicateAudios = newImageUrls.Where(url => url.Item2.Equals("Audio"))
                .GroupBy(url => url.Item1)
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count() - 1);

            blog.DuplicatePhotos = duplicatePhotos;
            blog.DuplicateVideos = duplicateVideos;
            blog.DuplicateAudios = duplicateAudios;
            blog.TotalCount = (blog.TotalCount - duplicatePhotos - duplicateAudios - duplicateVideos);

            bool finishedDownloading = consumer.Result;

            Task.WaitAll(producer, consumer);

            if (!ct.IsCancellationRequested)
            {
                blog.LastCompleteCrawl = DateTime.Now;
                if (finishedDownloading && !limitHit)
                    blog.LastId = blogContent.Item1;
            }

            blog.Dirty = false;
            blog.Save();

            newProgress = new DownloadProgress();
            newProgress.Progress = "";
            progress.Report(newProgress);
        }

        private void GetUrlsCore(List<Tuple<string, string, string>> downloadList, // only used for stats calculation
            BlockingCollection<Tuple<string, string, string>> bCollection, // used to store downloads
            XDocument document,
            List<string> tags,
            ref bool limitHit,
            ref int totalDownloads,
            ref int photos,
            ref int videos,
            ref int audios,
            ref int texts,
            ref int conversations,
            ref int quotes,
            ref int links,
            ref int photoMetas,
            ref int videoMetas,
            ref int audioMetas)
        {
            // FIXME: Remove the WET Code!
            // The same code block is once done with and without tags
            if (blog.DownloadPhoto == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "photo" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    // photoset
                    if (post.Descendants("photoset").Count() > 0)
                    {
                        foreach (var photo in post.Descendants("photoset").Descendants("photo"))
                        {
                            var imageUrl = photo.Elements("photo-url").Where(photo_url =>
                                photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).FirstOrDefault().Value;

                            if (blog.SkipGif == true && imageUrl.EndsWith(".gif"))
                                continue;

                            if (blog.ForceSize)
                            {
                                imageUrl = resizeTumblrImageUrl(imageUrl);
                            }

                            Interlocked.Increment(ref photos);
                            Interlocked.Increment(ref totalDownloads);
                            Monitor.Enter(downloadList);
                            downloadList.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                            Monitor.Exit(downloadList);

                            bCollection.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                        }
                    }
                    // single image
                    else
                    {
                        var imageUrl = post.Elements("photo-url").Where(photo_url =>
                                photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).FirstOrDefault().Value;

                        if (blog.SkipGif == true && imageUrl.EndsWith(".gif"))
                            continue;

                        if (blog.ForceSize)
                        {
                            imageUrl = resizeTumblrImageUrl(imageUrl);
                        }

                        Interlocked.Increment(ref photos);
                        Interlocked.Increment(ref totalDownloads);
                        Monitor.Enter(downloadList);
                        downloadList.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                        Monitor.Exit(downloadList);

                        bCollection.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                    }
                }
                // check for inline images
                foreach (var post in document.Descendants("post").Where(posts => true &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    if (String.Concat(post.Nodes()).Contains("tumblr_inline"))
                    {
                        Regex regex = new Regex("<img src=\"(.*?)\"");
                        foreach (Match match in regex.Matches(post.Element("regular-body")?.Value))
                        {
                            var imageUrl = match.Groups[1].Value;
                            if (blog.ForceSize)
                            {
                                imageUrl = resizeTumblrImageUrl(imageUrl);
                            }
                            Interlocked.Increment(ref photos);
                            Interlocked.Increment(ref totalDownloads);
                            Monitor.Enter(downloadList);
                            downloadList.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                            Monitor.Exit(downloadList);

                            bCollection.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                        }
                    }
                }
            }
            if (blog.DownloadVideo == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "video" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    var videoUrl = post.Descendants("video-player").Where(x => x.Value.Contains("<source src=")).Select(result =>
                        System.Text.RegularExpressions.Regex.Match(
                        result.Value, "<source src=\"(.*)\" type=\"video/mp4\">").Groups[1].Value).ToList();

                    foreach (string video in videoUrl)
                    {
                        if (shellService.Settings.VideoSize == 1080)
                        {
                            Interlocked.Increment(ref totalDownloads);
                            Monitor.Enter(downloadList);
                            downloadList.Add(Tuple.Create(video.Replace("/480", "") + ".mp4", "Video", post.Attribute("id").Value));
                            Monitor.Exit(downloadList);

                            bCollection.Add(Tuple.Create(video.Replace("/480", "") + ".mp4", "Video", post.Attribute("id").Value));
                        }
                        else if (shellService.Settings.VideoSize == 480)
                        {
                            Interlocked.Increment(ref totalDownloads);
                            Monitor.Enter(downloadList);
                            downloadList.Add(Tuple.Create("https://vt.tumblr.com/" + video.Replace("/480", "").Split('/').Last() + "_480.mp4", "Video", post.Attribute("id").Value));
                            Monitor.Exit(downloadList);

                            bCollection.Add(Tuple.Create("https://vt.tumblr.com/" + video.Replace("/480", "").Split('/').Last() + "_480.mp4", "Video", post.Attribute("id").Value));
                        }
                    }
                }
            }
            if (blog.DownloadAudio == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "audio" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    var audioUrl = post.Descendants("audio-player").Where(x => x.Value.Contains("src=")).Select(result =>
                        System.Text.RegularExpressions.Regex.Match(
                        result.Value, "src=\"(.*)\" height").Groups[1].Value).ToList();

                    foreach (string audiofile in audioUrl)
                    {
                        Interlocked.Increment(ref totalDownloads);
                        Monitor.Enter(downloadList);
                        downloadList.Add(Tuple.Create(WebUtility.UrlDecode(audiofile), "Audio", post.Attribute("id").Value));
                        Monitor.Exit(downloadList);

                        bCollection.Add(Tuple.Create(WebUtility.UrlDecode(audiofile), "Audio", post.Attribute("id").Value));
                    }

                }
            }
            if (blog.DownloadText == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "regular" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Title, post.Element("regular-title")?.Value) +
                        Environment.NewLine + post.Element("regular-body")?.Value +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                        Environment.NewLine;
                    Interlocked.Increment(ref totalDownloads);
                    bCollection.Add(Tuple.Create(textBody, "Text", post.Attribute("id").Value));
                }
            }
            if (blog.DownloadQuote == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "quote" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.Element("quote-text")?.Value) +
                        Environment.NewLine + post.Element("quote-source")?.Value +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                        Environment.NewLine;
                    Interlocked.Increment(ref totalDownloads);
                    bCollection.Add(Tuple.Create(textBody, "Quote", post.Attribute("id").Value));
                }
            }
            if (blog.DownloadLink == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "link" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Link, post.Element("link-text")?.Value) +
                        Environment.NewLine + post.Element("link-url")?.Value +
                        Environment.NewLine + post.Element("link-description")?.Value +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                        Environment.NewLine;
                    Interlocked.Increment(ref totalDownloads);
                    bCollection.Add(Tuple.Create(textBody, "Link", post.Attribute("id").Value));
                }
            }
            if (blog.DownloadConversation == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "conversation" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Conversation, post.Element("conversation-text")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                        Environment.NewLine;
                    Interlocked.Increment(ref totalDownloads);
                    bCollection.Add(Tuple.Create(textBody, "Conversation", post.Attribute("id").Value));
                }
            }
            if (blog.CreatePhotoMeta == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "photo" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string imageUrl = "";
                    // photoset
                    if (post.Descendants("photoset").Count() > 0)
                    {
                        List<string> imageUrls = new List<string>();
                        foreach (var photo in post.Descendants("photoset").Descendants("photo"))
                        {
                            var singleImageUrl = photo.Elements("photo-url").Where(photo_url =>
                                photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).FirstOrDefault().Value;

                            if (blog.SkipGif == true && imageUrl.EndsWith(".gif"))
                                continue;
                            imageUrls.Add(singleImageUrl);
                        }
                        // imageUrl = imageUrls.Aggregate((current, next) => current + ", " + next);
                        imageUrl = string.Join(", ", imageUrls.ToArray());
                    }
                    // single image
                    {
                        imageUrl = post.Elements("photo-url").Where(photo_url =>
                                photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).FirstOrDefault().Value;

                        if (blog.SkipGif == true && imageUrl.EndsWith(".gif"))
                            continue;

                        string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                            Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                            Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                            Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.PhotoUrl, imageUrl) +
                            Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.PhotoCaption, post.Element("photo-caption")?.Value) +
                            Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                            Environment.NewLine;
                        Interlocked.Increment(ref totalDownloads);
                        Interlocked.Increment(ref photoMetas);
                        bCollection.Add(Tuple.Create(textBody, "PhotoMeta", post.Attribute("id").Value));
                    }
                }
            }
            if (blog.CreateVideoMeta == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "video" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.VideoPlayer, post.Element("video-player")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                        Environment.NewLine;
                    Interlocked.Increment(ref totalDownloads);
                    Interlocked.Increment(ref videoMetas);
                    bCollection.Add(Tuple.Create(textBody, "VideoMeta", post.Attribute("id").Value));
                }
            }
            if (blog.CreateAudioMeta == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "audio" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.AudioCaption, post.Element("audio-caption")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Artist, post.Element("id3-artist")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Title, post.Element("id3-title")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.Element("id3-track")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Album, post.Element("id3-album")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Year, post.Element("id3-year")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                        Environment.NewLine;
                    Interlocked.Increment(ref totalDownloads);
                    Interlocked.Increment(ref audioMetas);
                    bCollection.Add(Tuple.Create(textBody, "AudioMeta", post.Attribute("id").Value));
                }
            }
        }

        public Tuple<ulong, bool, List<Tuple<string, string, string>>> GetUrls(BlockingCollection<Tuple<string, string, string>> bCollection, IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            int totalPosts = 0;
            int totalDownloads = 0;
            int numberOfPostsCrawled = 0;
            ulong highestId = 0;
            int photos = 0;
            int videos = 0;
            int audios = 0;
            int texts = 0;
            int conversations = 0;
            int quotes = 0;
            int links = 0;
            int photoMetas = 0;
            int videoMetas = 0;
            int audioMetas = 0;
            ulong lastId = blog.LastId;
            bool limitHit = false;
            List<string> tags = new List<string>();
            List<Tuple<string, string, string>> downloadList = new List<Tuple<string, string, string>>();

            string postCountUrl = GetApiUrl(blog.Url, 1);

            XDocument blogDoc = null;

            if (blog.ForceRescan)
            {
                blog.ForceRescan = false;
                lastId = 0;
            }

            if (shellService.Settings.LimitConnections)
            {
                crawlerService.Timeconstraint.Acquire();
                blogDoc = RequestData(postCountUrl);
            }
            else
                blogDoc = RequestData(postCountUrl);

            Int32.TryParse(blogDoc.Element("tumblr").Element("posts").Attribute("total").Value, out totalPosts);

            UInt64.TryParse(blogDoc.Element("tumblr").Element("posts").Element("post")?.Attribute("id").Value, out highestId);

            // Generate URL list of Images
            // the api shows 50 posts at max, determine the number of pages to crawl
            int totalPages = (totalPosts / 50) + 1;

            var loopState = Parallel.For(0, totalPages,
                        new ParallelOptions { MaxDegreeOfParallelism = (shellService.Settings.ParallelScans) },
                        (i, state) =>
                        {
                            if (ct.IsCancellationRequested)
                            {
                                state.Break();
                            }
                            if (pt.IsPaused)
                                pt.WaitWhilePausedWithResponseAsyc().Wait();
                            try
                            {
                                if (!String.IsNullOrWhiteSpace(blog.Tags))
                                    tags = blog.Tags.Split(',').Select(x => x.Trim()).ToList();

                                XDocument document = null;

                                // get 50 posts per crawl/page
                                string url = GetApiUrl(blog.Url, 50, i * 50);

                                if (shellService.Settings.LimitConnections)
                                {
                                    crawlerService.Timeconstraint.Acquire();
                                    document = RequestData(url);
                                }
                                else
                                    document = RequestData(url);

                                if (document == null)
                                    limitHit = true;

                                // Doesn't count photosets
                                //Interlocked.Add(ref photos, document.Descendants("post").Where(post => post.Attribute("type").Value == "photo").Count());
                                Interlocked.Add(ref videos, document.Descendants("post").Where(post => post.Attribute("type").Value == "video").Count());
                                Interlocked.Add(ref audios, document.Descendants("post").Where(post => post.Attribute("type").Value == "audio").Count());
                                Interlocked.Add(ref texts, document.Descendants("post").Where(post => post.Attribute("type").Value == "regular").Count());
                                Interlocked.Add(ref conversations, document.Descendants("post").Where(post => post.Attribute("type").Value == "conversation").Count());
                                Interlocked.Add(ref quotes, document.Descendants("post").Where(post => post.Attribute("type").Value == "quote").Count());
                                Interlocked.Add(ref links, document.Descendants("post").Where(post => post.Attribute("type").Value == "link").Count());

                                ulong highestPostId = 0;
                                UInt64.TryParse(blogDoc.Element("tumblr").Element("posts").Element("post")?.Attribute("id").Value, out highestId);

                                if (highestPostId < lastId)
                                    state.Break();

                                GetUrlsCore(downloadList, bCollection, document, tags, ref limitHit, ref totalDownloads, ref photos, ref videos,
                                    ref audios, ref texts, ref conversations, ref quotes, ref links, ref photoMetas, ref videoMetas, ref audioMetas);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Data);
                            }

                            numberOfPostsCrawled += 50;
                            var newProgress = new DownloadProgress();
                            newProgress.Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressGetUrl, numberOfPostsCrawled, totalPosts);
                            progress.Report(newProgress);
                        }
                );

            bCollection.CompleteAdding();

            if (loopState.IsCompleted)
            {
                blog.TotalCount = totalDownloads;
                blog.Posts = totalPosts;
                blog.Photos = photos;
                blog.Videos = videos;
                blog.Audios = audios;
                blog.Texts = texts;
                blog.Conversations = conversations;
                blog.Quotes = quotes;
                blog.NumberOfLinks = links;
                blog.PhotoMetas = photoMetas;
                blog.VideoMetas = videoMetas;
                blog.AudioMetas = audioMetas;
            }

            return Tuple.Create(highestId, limitHit, downloadList);
        }

        private TumblrFiles LoadTumblrFiles()
        {
            string filename = blog.ChildId;

            try
            {
                string json = File.ReadAllText(filename);
                System.Web.Script.Serialization.JavaScriptSerializer jsJson = new System.Web.Script.Serialization.JavaScriptSerializer();
                jsJson.MaxJsonLength = 2147483644;
                TumblrFiles files = jsJson.Deserialize<TumblrFiles>(json);
                return files;
            }
            catch (SerializationException ex)
            {
                ex.Data["Filename"] = filename;
                throw;
            }
        }


        private bool DownloadTumblrBlog(BlockingCollection<Tuple<string, string, string>> bCollection, IProgress<DataModels.DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {

            object lockObjectProgress = new object();
            object lockObjectDownload = new object();
            bool locked = false;

            int downloadedImages = (int)blog.DownloadedImages;
            int downloadedPhotos = (int)blog.DownloadedPhotos;
            int downloadedVideos = (int)blog.DownloadedVideos;
            int downloadedAudios = (int)blog.DownloadedAudios;
            int downloadedTexts = (int)blog.DownloadedTexts;
            int downloadedQuotes = (int)blog.DownloadedQuotes;
            int downloadedLinks = (int)blog.DownloadedLinks;
            int downloadedConversations = (int)blog.DownloadedConversations;
            int downloadedPhotoMetas = (int)blog.DownloadedPhotoMetas;
            int downloadedVideoMetas = (int)blog.DownloadedVideoMetas;
            int downloadedAudioMetas = (int)blog.DownloadedAudioMetas;

            var indexPath = Path.Combine(shellService.Settings.DownloadLocation, "Index");
            var blogPath = shellService.Settings.DownloadLocation;

            // make sure the datafolder still exists
            CreateDataFolder(blog.Name, blogPath);

            blog.ChildId = Path.Combine(indexPath, blog.Name + "_files.tumblr");
            TumblrFiles files = LoadTumblrFiles();
            files.Name = blog.Name;
            files.Location = indexPath;

            var loopState = Parallel.ForEach(
                            bCollection.GetConsumingEnumerable(),
                            new ParallelOptions { MaxDegreeOfParallelism = (shellService.Settings.ParallelImages / selectionService.ActiveItems.Count) },
                            (currentImageUrl, state) =>
                            {
                                if (ct.IsCancellationRequested)
                                {
                                    state.Break();
                                }
                                if (pt.IsPaused)
                                    pt.WaitWhilePausedWithResponseAsyc().Wait();

                                string fileName = String.Empty;
                                string fileLocation = String.Empty;

                                switch (currentImageUrl.Item2)
                                {
                                    case "Photo":

                                        fileName = currentImageUrl.Item1.Split('/').Last();
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), fileName);

                                        if (Download(files, fileLocation, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedPhotos, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, fileName, lockObjectProgress, ref downloadedImages);
                                            blog.DownloadedPhotos = downloadedPhotos;
                                            if (shellService.Settings.EnablePreview)
                                            {
                                                if (!fileName.EndsWith(".gif"))
                                                    blog.LastDownloadedPhoto = Path.GetFullPath(fileLocation);
                                                else
                                                    blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);
                                            }
                                        }
                                        break;
                                    case "Video":
                                        fileName = currentImageUrl.Item1.Split('/').Last();
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), fileName);

                                        if (Download(files, fileLocation, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedVideos, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, fileName, lockObjectProgress, ref downloadedImages);
                                            if (shellService.Settings.EnablePreview)
                                                blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);
                                            blog.DownloadedVideos = downloadedVideos;
                                        }
                                        break;
                                    case "Audio":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), currentImageUrl.Item3 + ".swf");
                                        if (Download(files, fileLocation, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedAudios, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item1, lockObjectProgress, ref downloadedImages);
                                            blog.DownloadedAudios = downloadedAudios;
                                        }
                                        break;
                                    case "Text":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameTexts));

                                        if (Download(files, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedTexts, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item3, lockObjectProgress, ref downloadedImages);
                                            blog.DownloadedTexts = downloadedTexts;
                                        }
                                        break;
                                    case "Quote":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameQuotes));

                                        if (Download(files, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedQuotes, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item3, lockObjectProgress, ref downloadedImages);
                                            blog.DownloadedQuotes = downloadedQuotes;
                                        }
                                        break;
                                    case "Link":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameLinks));

                                        if (Download(files, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedLinks, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item3, lockObjectProgress, ref downloadedImages);
                                            blog.DownloadedLinks = downloadedLinks;
                                        }
                                        break;
                                    case "Conversation":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameConversations));

                                        if (Download(files, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedConversations, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item3, lockObjectProgress, ref downloadedImages);
                                            blog.DownloadedConversations = downloadedConversations;
                                        }
                                        break;
                                    case "PhotoMeta":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaPhoto));

                                        if (Download(files, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedPhotoMetas, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item3, lockObjectProgress, ref downloadedImages);
                                            blog.DownloadedPhotoMetas = downloadedPhotoMetas;
                                        }
                                        break;
                                    case "VideoMeta":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaVideo));

                                        if (Download(files, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedVideoMetas, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item3, lockObjectProgress, ref downloadedImages);
                                            blog.DownloadedVideoMetas = downloadedVideoMetas;
                                        }
                                        break;
                                    case "AudioMeta":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaAudio));

                                        if (Download(files, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedAudioMetas, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item3, lockObjectProgress, ref downloadedImages);
                                            blog.DownloadedAudioMetas = downloadedAudioMetas;
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            });

            blog.LastDownloadedPhoto = null;
            blog.LastDownloadedVideo = null;

            files.Save();
            files = null;

            if (loopState.IsCompleted)
                return true;

            return false;
        }
    }
}
