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
using System.ComponentModel.Composition;

namespace TumblThree.Applications.Downloader
{
    [Export(typeof(IDownloader))]
    [ExportMetadata("BlogType", BlogTypes.tumblr)]
    public class TumblrDownloader : Downloader
    {

        private readonly IShellService shellService;
        private readonly ICrawlerService crawlerService;
        private readonly TumblrBlog blog;
        private TumblrFiles files;
        private readonly object lockObjectProgress;
        private readonly object lockObjectDownload;
        private readonly List<Tuple<PostTypes, string, string>> downloadList;
        private readonly BlockingCollection<Tuple<PostTypes, string, string>> sharedDownloads;


        public TumblrDownloader(IShellService shellService, ICrawlerService crawlerService, IBlog blog) : base(shellService, blog)
        {
            this.shellService = shellService;
            this.crawlerService = crawlerService;
            this.blog = (TumblrBlog)blog;
            this.files = LoadTumblrFiles();
            this.lockObjectProgress = new object();
            this.lockObjectDownload = new object();
            this.downloadList = new List<Tuple<PostTypes, string, string>>();
            this.sharedDownloads = new BlockingCollection<Tuple<PostTypes, string, string>>();
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

        private string GetApiUrl(string url, int count, int start = 0)
        {
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

        private XDocument GetApiPage(int pageId)
        {
            XDocument document = null;

            string url = GetApiUrl(blog.Url, 50, pageId * 50);

            if (shellService.Settings.LimitConnections)
            {
                crawlerService.Timeconstraint.Acquire();
                document = RequestData(url);
            }
            else {
                document = RequestData(url);
            }
            return document;
        }

        public Task UpdateMetaInformation()
        {
            return Task.Factory.StartNew(() =>
            {
                XDocument document = GetApiPage(1);

                if (document != null)
                {
                    blog.Title = document.Element("tumblr").Element("tumblelog").Attribute("title")?.Value;
                    blog.Description = document.Element("tumblr").Element("tumblelog")?.Value;
                    blog.TotalCount = Int32.Parse(document.Element("tumblr").Element("posts").Attribute("total")?.Value);
                }
            },
            TaskCreationOptions.LongRunning);
        }

        private string ResizeTumblrImageUrl(string imageUrl)
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

        private int DetermineDuplicates(IEnumerable<Tuple<PostTypes, string, string>> source, PostTypes type)
        {
            return source.Where(url => url.Item1.Equals(type))
                .GroupBy(url => url.Item2)
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count() - 1);
        }

        public void Crawl(IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            Logger.Verbose("TumblrDownloader.Crawl:Start");

            var grabber = Task.Run(() => GetUrls(progress, ct, pt));
            var downloader = Task.Run(() => DownloadTumblrBlog(progress, ct, pt));
            var blogContent = grabber.Result;
            bool apiLimitHit = blogContent.Item2;
            var blogUrls = blogContent.Item3;

            var newProgress = new DownloadProgress();
            newProgress.Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressUniqueDownloads);
            progress.Report(newProgress);

            int duplicatePhotos = DetermineDuplicates(blogUrls, PostTypes.Photo);
            int duplicateVideos = DetermineDuplicates(blogUrls, PostTypes.Video);
            int duplicateAudios = DetermineDuplicates(blogUrls, PostTypes.Audio);

            blog.DuplicatePhotos = duplicatePhotos;
            blog.DuplicateVideos = duplicateVideos;
            blog.DuplicateAudios = duplicateAudios;
            blog.TotalCount = (blog.TotalCount - duplicatePhotos - duplicateAudios - duplicateVideos);

            bool finishedDownloading = downloader.Result;

            Task.WaitAll(grabber, downloader);

            if (!ct.IsCancellationRequested)
            {
                blog.LastCompleteCrawl = DateTime.Now;
                if (finishedDownloading && !apiLimitHit)
                    blog.LastId = blogContent.Item1;
            }

            blog.Save();

            newProgress = new DownloadProgress();
            newProgress.Progress = "";
            progress.Report(newProgress);
        }

        private void UpdateTotalPostCount()
        {

            XDocument document = GetApiPage(1);

            int totalPosts = 0;
            Int32.TryParse(document.Element("tumblr").Element("posts").Attribute("total").Value, out totalPosts);
            blog.Posts = totalPosts;
        }

        private ulong GetHighestPostId()
        {
            XDocument document = GetApiPage(1);

            ulong highestId = 0;
            UInt64.TryParse(document.Element("tumblr").Element("posts").Element("post")?.Attribute("id").Value, out highestId);
            return highestId;
        }

        protected override bool CheckIfFileExistsInDB(string url)
        {
            var fileName = url.Split('/').Last();
            Monitor.Enter(lockObject);
            if (files.Links.Contains(fileName))
            {
                Monitor.Exit(lockObject);
                return true;
            }
            Monitor.Exit(lockObject);
            return false;
        }

        public Tuple<ulong, bool, List<Tuple<PostTypes, string, string>>> GetUrls(IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            int totalDownloads = 0;
            int numberOfPostsCrawled = 0;
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
            bool apiLimitHit = false;

            if (blog.ForceRescan)
            {
                blog.ForceRescan = false;
                lastId = 0;
            }

            UpdateTotalPostCount();
            int totalPosts = blog.Posts;

            ulong highestId = GetHighestPostId();

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
                                XDocument document = GetApiPage(i);

                                if (document == null)
                                {
                                    apiLimitHit = true;
                                }

                                // only counts single images and photoset, no inline images
                                //Interlocked.Add(ref photos, document.Descendants("post").Where(post => post.Attribute("type").Value == "photo").Count());
                                //Interlocked.Add(ref photos, document.Descendants("photo").Count() - 1);
                                Interlocked.Add(ref videos, document.Descendants("post").Where(post => post.Attribute("type").Value == "video").Count());
                                Interlocked.Add(ref audios, document.Descendants("post").Where(post => post.Attribute("type").Value == "audio").Count());
                                Interlocked.Add(ref texts, document.Descendants("post").Where(post => post.Attribute("type").Value == "regular").Count());
                                Interlocked.Add(ref conversations, document.Descendants("post").Where(post => post.Attribute("type").Value == "conversation").Count());
                                Interlocked.Add(ref quotes, document.Descendants("post").Where(post => post.Attribute("type").Value == "quote").Count());
                                Interlocked.Add(ref links, document.Descendants("post").Where(post => post.Attribute("type").Value == "link").Count());

                                ulong highestPostId = 0;
                                UInt64.TryParse(document.Element("tumblr").Element("posts").Element("post")?.Attribute("id").Value, out highestId);

                                if (highestPostId < lastId)
                                    state.Break();

                                GetUrlsCore(document, ref totalDownloads, ref photos, ref photoMetas, ref videoMetas, ref audioMetas);
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

            sharedDownloads.CompleteAdding();

            if (loopState.IsCompleted)
            {
                blog.TotalCount = totalDownloads;
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

            return Tuple.Create(highestId, apiLimitHit, downloadList);
        }

        private void GetUrlsCore(XDocument document,
            ref int totalDownloads,
            ref int photos,
            ref int photoMetas,
            ref int videoMetas,
            ref int audioMetas)
        {
            List<string> tags = new List<string>();
            if (!String.IsNullOrWhiteSpace(blog.Tags))
            {
                tags = blog.Tags.Split(',').Select(x => x.Trim()).ToList();
            }

            // FIXME: Everything but not SOLID
            // Add Conditional with Polymorphism
            // Just use regex instead?
            if (blog.DownloadPhoto)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "photo" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    // photoset
                    if (post.Descendants("photoset").Count() > 0)
                    {
                        foreach (var photo in post.Descendants("photoset").Descendants("photo"))
                        {
                            string imageUrl = ParseImageUrl(photo);
                            if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                                continue;

                            UpdateBlogCounter(ref photos, ref totalDownloads);
                            AddToDownloadList(Tuple.Create(PostTypes.Photo, imageUrl, post.Attribute("id").Value));
                            sharedDownloads.Add(Tuple.Create(PostTypes.Photo, imageUrl, post.Attribute("id").Value));
                        }
                    }
                    // single image
                    else
                    {
                        string imageUrl = ParseImageUrl(post);
                        if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                            continue;

                        UpdateBlogCounter(ref photos, ref totalDownloads);
                        AddToDownloadList(Tuple.Create(PostTypes.Photo, imageUrl, post.Attribute("id").Value));
                        sharedDownloads.Add(Tuple.Create(PostTypes.Photo, imageUrl, post.Attribute("id").Value));
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
                                imageUrl = ResizeTumblrImageUrl(imageUrl);
                            }
                            UpdateBlogCounter(ref photos, ref totalDownloads);
                            AddToDownloadList(Tuple.Create(PostTypes.Photo, imageUrl, post.Attribute("id").Value));
                            sharedDownloads.Add(Tuple.Create(PostTypes.Photo, imageUrl, post.Attribute("id").Value));
                        }
                    }
                }
            }
            if (blog.DownloadVideo)
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
                            AddToDownloadList(Tuple.Create(PostTypes.Video, video.Replace("/480", "") + ".mp4", post.Attribute("id").Value));
                            sharedDownloads.Add(Tuple.Create(PostTypes.Video, video.Replace("/480", "") + ".mp4", post.Attribute("id").Value));
                        }
                        else if (shellService.Settings.VideoSize == 480)
                        {
                            Interlocked.Increment(ref totalDownloads);
                            AddToDownloadList(Tuple.Create(PostTypes.Video, "https://vt.tumblr.com/" + video.Replace("/480", "").Split('/').Last() + "_480.mp4", post.Attribute("id").Value));
                            sharedDownloads.Add(Tuple.Create(PostTypes.Video, "https://vt.tumblr.com/" + video.Replace("/480", "").Split('/').Last() + "_480.mp4", post.Attribute("id").Value));
                        }
                    }
                }
            }
            if (blog.DownloadAudio)
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
                        AddToDownloadList(Tuple.Create(PostTypes.Audio, WebUtility.UrlDecode(audiofile), post.Attribute("id").Value));
                        sharedDownloads.Add(Tuple.Create(PostTypes.Audio, WebUtility.UrlDecode(audiofile), post.Attribute("id").Value));
                    }

                }
            }
            if (blog.DownloadText)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "regular" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = ParseText(post);
                    Interlocked.Increment(ref totalDownloads);
                    sharedDownloads.Add(Tuple.Create(PostTypes.Text, textBody, post.Attribute("id").Value));
                }
            }
            if (blog.DownloadQuote)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "quote" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = ParseQuote(post);
                    Interlocked.Increment(ref totalDownloads);
                    sharedDownloads.Add(Tuple.Create(PostTypes.Quote, textBody, post.Attribute("id").Value));
                }
            }
            if (blog.DownloadLink)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "link" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = ParseLink(post);
                    Interlocked.Increment(ref totalDownloads);
                    sharedDownloads.Add(Tuple.Create(PostTypes.Link, textBody, post.Attribute("id").Value));
                }
            }
            if (blog.DownloadConversation)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "conversation" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = ParseConversation(post);
                    Interlocked.Increment(ref totalDownloads);
                    sharedDownloads.Add(Tuple.Create(PostTypes.Conversation, textBody, post.Attribute("id").Value));
                }
            }
            if (blog.CreatePhotoMeta)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "photo" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = ParsePhotoMeta(post);
                    Interlocked.Increment(ref totalDownloads);
                    Interlocked.Increment(ref photoMetas);
                    sharedDownloads.Add(Tuple.Create(PostTypes.PhotoMeta, textBody,  post.Attribute("id").Value));
                }
            }
            if (blog.CreateVideoMeta)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "video" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = ParseVideoMeta(post);
                    Interlocked.Increment(ref totalDownloads);
                    Interlocked.Increment(ref videoMetas);
                    sharedDownloads.Add(Tuple.Create(PostTypes.VideoMeta, textBody, post.Attribute("id").Value));
                }
            }
            if (blog.CreateAudioMeta)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "audio" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = ParseAudioMeta(post);
                    Interlocked.Increment(ref totalDownloads);
                    Interlocked.Increment(ref audioMetas);
                    sharedDownloads.Add(Tuple.Create(PostTypes.AudioMeta, textBody, post.Attribute("id").Value));
                }
            }
        }

        private void AddToDownloadList(Tuple<PostTypes, string, string> addToList)
        {
            Monitor.Enter(downloadList);
            downloadList.Add(addToList);
            Monitor.Exit(downloadList);
        }

        private string ParseImageUrl(XElement post)
        {
            var imageUrl = post.Elements("photo-url").Where(photo_url =>
                photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).FirstOrDefault().Value;

            if (blog.ForceSize)
            {
                imageUrl = ResizeTumblrImageUrl(imageUrl);
            }

            return imageUrl;
        }

        private string ParsePhotoMeta(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.PhotoUrl, post.Element("photo-url").Value) +
                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.PhotoCaption, post.Element("photo-caption")?.Value) +
                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                Environment.NewLine;
        }

        private string ParseVideoMeta(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.VideoPlayer, post.Element("video-player")?.Value) +
                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                Environment.NewLine;
        }

        private string ParseAudioMeta(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
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
        }

        private string ParseConversation(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Conversation, post.Element("conversation-text")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                        Environment.NewLine;
        }

        private string ParseLink(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Link, post.Element("link-text")?.Value) +
                        Environment.NewLine + post.Element("link-url")?.Value +
                        Environment.NewLine + post.Element("link-description")?.Value +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                        Environment.NewLine;
        }

        private string ParseQuote(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.Element("quote-text")?.Value) +
                        Environment.NewLine + post.Element("quote-source")?.Value +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                        Environment.NewLine;
        }

        private string ParseText(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Title, post.Element("regular-title")?.Value) +
                        Environment.NewLine + post.Element("regular-body")?.Value +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                        Environment.NewLine;
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

        private bool DownloadTumblrBlog(IProgress<DataModels.DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            int downloadedFiles = blog.DownloadedImages;
            int downloadedPhotos = blog.DownloadedPhotos;
            int downloadedVideos = blog.DownloadedVideos;
            int downloadedAudios = blog.DownloadedAudios;
            int downloadedTexts = blog.DownloadedTexts;
            int downloadedQuotes = blog.DownloadedQuotes;
            int downloadedLinks = blog.DownloadedLinks;
            int downloadedConversations = blog.DownloadedConversations;
            int downloadedPhotoMetas = blog.DownloadedPhotoMetas;
            int downloadedVideoMetas = blog.DownloadedVideoMetas;
            int downloadedAudioMetas = blog.DownloadedAudioMetas;

            string blogPath = Directory.GetParent(blog.Location).FullName;

            CreateDataFolder();

            var loopState = Parallel.ForEach(
                            sharedDownloads.GetConsumingEnumerable(),
                            new ParallelOptions { MaxDegreeOfParallelism = (shellService.Settings.ParallelImages / crawlerService.ActiveItems.Count) },
                            (currentImageUrl, state) =>
                            {
                                if (ct.IsCancellationRequested)
                                {
                                    state.Break();
                                }
                                if (pt.IsPaused)
                                    pt.WaitWhilePausedWithResponseAsyc().Wait();

                                string fileName = String.Empty;
                                string url = String.Empty;
                                string fileLocation = String.Empty;
                                string postId = String.Empty;

                                // FIXME: Conditional with Polymorphism
                                switch (currentImageUrl.Item1)
                                {
                                    case PostTypes.Photo:
                                        fileName = currentImageUrl.Item2.Split('/').Last();
                                        url = currentImageUrl.Item2;
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), fileName);

                                        if (!(CheckIfFileExistsInDB(url) || CheckIfBlogShouldCheckDirectory(url)))
                                        {
                                            UpdateProgressQueueInformation(progress, fileName);
                                            DownloadBinaryFile(fileLocation, url);
                                            UpdateBlogCounter(ref downloadedPhotos, ref downloadedFiles);
                                            UpdateBlogProgress(fileName, ref downloadedFiles);
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
                                    case PostTypes.Video:
                                        fileName = currentImageUrl.Item2.Split('/').Last();
                                        url = currentImageUrl.Item2;
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), fileName);

                                        if (!(CheckIfFileExistsInDB(url) || CheckIfBlogShouldCheckDirectory(url)))
                                        {
                                            UpdateProgressQueueInformation(progress, fileName);
                                            DownloadBinaryFile(fileLocation, url);
                                            UpdateBlogCounter(ref downloadedVideos, ref downloadedFiles);
                                            UpdateBlogProgress(fileName, ref downloadedFiles);
                                            blog.DownloadedVideos = downloadedVideos;
                                            if (shellService.Settings.EnablePreview)
                                            {
                                                blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);
                                            }
                                        }
                                        break;
                                    case PostTypes.Audio:
                                        fileName = currentImageUrl.Item2.Split('/').Last();
                                        url = currentImageUrl.Item2;
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), currentImageUrl.Item3 + ".swf");

                                        if (!(CheckIfFileExistsInDB(url) || CheckIfBlogShouldCheckDirectory(url)))
                                        {
                                            UpdateProgressQueueInformation(progress, fileName);
                                            DownloadBinaryFile(fileLocation, url);
                                            UpdateBlogCounter(ref downloadedAudios, ref downloadedFiles);
                                            UpdateBlogProgress(fileName, ref downloadedFiles);
                                            blog.DownloadedAudios = downloadedAudios;
                                        }
                                        break;
                                    case PostTypes.Text:
                                        url = currentImageUrl.Item2;
                                        postId = currentImageUrl.Item3;
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameTexts));

                                        if (!CheckIfFileExistsInDB(postId))
                                        {
                                            UpdateProgressQueueInformation(progress, postId);
                                            AppendToTextFile(fileLocation, url);
                                            UpdateBlogCounter(ref downloadedTexts, ref downloadedFiles);
                                            UpdateBlogProgress(postId, ref downloadedFiles);
                                            blog.DownloadedTexts = downloadedTexts;
                                        }
                                        break;
                                    case PostTypes.Quote:
                                        url = currentImageUrl.Item2;
                                        postId = currentImageUrl.Item3;
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameQuotes));

                                        if (!CheckIfFileExistsInDB(postId))
                                        {
                                            UpdateProgressQueueInformation(progress, postId);
                                            AppendToTextFile(fileLocation, url);
                                            UpdateBlogCounter(ref downloadedQuotes, ref downloadedFiles);
                                            UpdateBlogProgress(postId, ref downloadedFiles);
                                            blog.DownloadedQuotes = downloadedQuotes;
                                        }
                                        break;
                                    case PostTypes.Link:
                                        url = currentImageUrl.Item2;
                                        postId = currentImageUrl.Item3;
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameLinks));

                                        if (!CheckIfFileExistsInDB(postId))
                                        {
                                            UpdateProgressQueueInformation(progress, postId);
                                            AppendToTextFile(fileLocation, url);
                                            UpdateBlogCounter(ref downloadedLinks, ref downloadedFiles);
                                            UpdateBlogProgress(postId, ref downloadedFiles);
                                            blog.DownloadedLinks = downloadedLinks;
                                        }
                                        break;
                                    case PostTypes.Conversation:
                                        url = currentImageUrl.Item2;
                                        postId = currentImageUrl.Item3;
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameConversations));

                                        if (!CheckIfFileExistsInDB(postId))
                                        {
                                            UpdateProgressQueueInformation(progress, postId);
                                            AppendToTextFile(fileLocation, url);
                                            UpdateBlogCounter(ref downloadedConversations, ref downloadedFiles);
                                            UpdateBlogProgress(postId, ref downloadedFiles);
                                            blog.DownloadedConversations = downloadedConversations;
                                        }
                                        break;
                                    case PostTypes.PhotoMeta:
                                        url = currentImageUrl.Item2;
                                        postId = currentImageUrl.Item3;
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaPhoto));

                                        if (!CheckIfFileExistsInDB(postId))
                                        {
                                            UpdateProgressQueueInformation(progress, postId);
                                            AppendToTextFile(fileLocation, url);
                                            UpdateBlogCounter(ref downloadedPhotoMetas, ref downloadedFiles);
                                            UpdateBlogProgress(postId, ref downloadedFiles);
                                            blog.DownloadedPhotoMetas = downloadedPhotoMetas;
                                        }
                                        break;
                                    case PostTypes.VideoMeta:
                                        url = currentImageUrl.Item2;
                                        postId = currentImageUrl.Item3;
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaVideo));

                                        if (!CheckIfFileExistsInDB(postId))
                                        {
                                            UpdateProgressQueueInformation(progress, postId);
                                            AppendToTextFile(fileLocation, url);
                                            UpdateBlogCounter(ref downloadedVideoMetas, ref downloadedFiles);
                                            UpdateBlogProgress(postId, ref downloadedFiles);
                                            blog.DownloadedVideoMetas = downloadedVideoMetas;
                                        }
                                        break;
                                    case PostTypes.AudioMeta:
                                        url = currentImageUrl.Item2;
                                        postId = currentImageUrl.Item3;
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaAudio));

                                        if (!CheckIfFileExistsInDB(postId))
                                        {
                                            UpdateProgressQueueInformation(progress, postId);
                                            AppendToTextFile(fileLocation, url);
                                            UpdateBlogCounter(ref downloadedAudioMetas, ref downloadedFiles);
                                            UpdateBlogProgress(postId, ref downloadedFiles);
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

            if (loopState.IsCompleted)
                return true;

            return false;
        }

        protected virtual void UpdateBlogProgress(string fileName, ref int totalCounter)
        {
            lock (lockObjectProgress)
            {
                files.Links.Add(fileName);
                blog.DownloadedImages = totalCounter;
                blog.Progress = (int)((double)totalCounter / (double)blog.TotalCount * 100);
            }
        }
    }
}
