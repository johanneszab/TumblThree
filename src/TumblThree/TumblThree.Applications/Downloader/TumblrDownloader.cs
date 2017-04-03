using System;
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
using TumblThree.Domain;
using System.ComponentModel.Composition;

namespace TumblThree.Applications.Downloader
{
    [Export(typeof(IDownloader))]
    [ExportMetadata("BlogType", BlogTypes.tumblr)]
    public class TumblrDownloader : Downloader, IDownloader
    {

        private readonly IShellService shellService;
        private readonly ICrawlerService crawlerService;
        private readonly TumblrBlog blog;
        private readonly PostCounter counter;

        public TumblrDownloader(IShellService shellService, ICrawlerService crawlerService, IBlog blog) : base(shellService, crawlerService, blog)
        {
            this.shellService = shellService;
            this.crawlerService = crawlerService;
            this.blog = (TumblrBlog)blog;
            this.counter = new PostCounter();
        }

        private new async Task<XDocument> RequestDataAsync(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.ProtocolVersion = HttpVersion.Version11;
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                request.AllowAutoRedirect = true;
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.ReadWriteTimeout = shellService.Settings.TimeOut * 1000;
                request.Timeout = -1;
                ServicePointManager.DefaultConnectionLimit = 400;
                if (!String.IsNullOrEmpty(shellService.Settings.ProxyHost))
                {
                    request.Proxy = new WebProxy(shellService.Settings.ProxyHost, Int32.Parse(shellService.Settings.ProxyPort));
                }
                else
                {
                    request.Proxy = null;
                }

                int bandwidth = 2000000;
                if (shellService.Settings.LimitScanBandwidth)
                {
                    bandwidth = shellService.Settings.Bandwidth;
                }

                using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
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

        private async Task<XDocument> GetApiPageAsync(int pageId)
        {
            XDocument document;

            string url = GetApiUrl(blog.Url, 50, pageId * 50);

            if (shellService.Settings.LimitConnections)
            {
                crawlerService.Timeconstraint.Acquire();
                document = await RequestDataAsync(url);
            }
            else {
                document = await RequestDataAsync(url);
            }
            return document;
        }

        public override async Task UpdateMetaInformationAsync()
        {
            XDocument document = await GetApiPageAsync(1);

            if (document.Root.Descendants().Any())
            {
                blog.Title = document.Element("tumblr").Element("tumblelog").Attribute("title")?.Value;
                blog.Description = document.Element("tumblr").Element("tumblelog")?.Value;
                blog.TotalCount = Int32.Parse(document.Element("tumblr").Element("posts").Attribute("total")?.Value);
            }
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

        /// <returns>
        /// Return the url without the size and type suffix (e.g. https://68.media.tumblr.com/51a99943f4aa7068b6fd9a6b36e4961b/tumblr_mnj6m9Huml1qat3lvo1).
        /// </returns>
        protected override string GetCoreImageUrl(string url)
        {
            return url.Split('_')[0] + "_" + url.Split('_')[1];
        }

        private int DetermineDuplicates(PostTypes type)
        {
            return downloadList.Where(url => url.Item1.Equals(type))
                .GroupBy(url => url.Item2)
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count() - 1);
        }

        public void Crawl(IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            Logger.Verbose("TumblrDownloader.Crawl:Start");

            var grabber = Task.Run(() => GetUrls(progress, ct, pt));
            var downloader = Task.Run(() => DownloadBlog(progress, ct, pt));
            var grabberResult = grabber.Result;
            bool apiLimitHit = grabberResult.Item2;

            var newProgress = new DownloadProgress
            {
                Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressUniqueDownloads)
            };
            progress.Report(newProgress);

            int duplicatePhotos = DetermineDuplicates(PostTypes.Photo);
            int duplicateVideos = DetermineDuplicates(PostTypes.Video);
            int duplicateAudios = DetermineDuplicates(PostTypes.Audio);

            blog.DuplicatePhotos = duplicatePhotos;
            blog.DuplicateVideos = duplicateVideos;
            blog.DuplicateAudios = duplicateAudios;
            blog.TotalCount = (blog.TotalCount - duplicatePhotos - duplicateAudios - duplicateVideos);

            bool finishedDownloading = downloader.Result;

            if (!ct.IsCancellationRequested)
            {
                blog.LastCompleteCrawl = DateTime.Now;
                if (finishedDownloading && !apiLimitHit)
                    blog.LastId = grabberResult.Item1;
            }

            blog.Save();

            newProgress = new DownloadProgress { Progress = "" };
            progress.Report(newProgress);
        }

        private async Task UpdateTotalPostCount()
        {
            XDocument document = await GetApiPageAsync(1);

            int totalPosts;
            int.TryParse(document?.Element("tumblr").Element("posts").Attribute("total").Value, out totalPosts);
            blog.Posts = totalPosts;
        }

        private async Task<ulong> GetHighestPostId()
        {
            XDocument document = await GetApiPageAsync(1);

            ulong highestId;
            ulong.TryParse(document?.Element("tumblr").Element("posts").Element("post")?.Attribute("id").Value, out highestId);
            return highestId;
        }

        protected override bool CheckIfFileExistsInDB(string url)
        {
            string fileName = url.Split('/').Last();
            Monitor.Enter(lockObjectDb);
            if (files.Links.Contains(fileName))
            {
                Monitor.Exit(lockObjectDb);
                return true;
            }
            Monitor.Exit(lockObjectDb);
            return false;
        }

        private async Task<Tuple<ulong, bool>> GetUrls(IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            SemaphoreSlim semaphoreSlim = new SemaphoreSlim(shellService.Settings.ParallelScans);
            List<Task> trackedTasks = new List<Task>();
            int numberOfPostsCrawled = 0;
            bool apiLimitHit = false;
            bool loopCompleted = true;

            ulong lastId = blog.LastId;
            if (blog.ForceRescan)
            {
                blog.ForceRescan = false;
                lastId = 0;
            }

            await UpdateTotalPostCount();
            int totalPosts = blog.Posts;

            ulong highestId = await GetHighestPostId();

            // Generate URL list of Images
            // the api shows 50 posts at max, determine the number of pages to crawl
            int totalPages = (totalPosts / 50) + 1;

            foreach (int pageNumber in Enumerable.Range(0, totalPages))
            {
                await semaphoreSlim.WaitAsync();

                if (!loopCompleted)
                {
                    break;
                }

                if (ct.IsCancellationRequested)
                {
                    break;
                }
                if (pt.IsPaused)
                    pt.WaitWhilePausedWithResponseAsyc().Wait();

                trackedTasks.Add(new Func<Task>(async () =>
                    {
                        try
                        {
                            XDocument document = await GetApiPageAsync(pageNumber);

                            if (document.Root.Descendants().Any())
                            {
                                // add retry logic?
                                apiLimitHit = true;
                            }

                            CountPostTypes(document);

                            ulong highestPostId = 0;
                            ulong.TryParse(document.Element("tumblr").Element("posts").Element("post")?.Attribute("id").Value, out highestId);

                            if (highestPostId < lastId)
                                loopCompleted = false;

                            AddUrlsToDownloadList(document);
                        }
                        catch
                        {
                        }
                        finally
                        {
                            semaphoreSlim.Release();
                        }

                        numberOfPostsCrawled += 50;
                        var newProgress = new DownloadProgress();
                        newProgress.Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressGetUrl, numberOfPostsCrawled, totalPosts);
                        progress.Report(newProgress);
                    })());
            }
            await Task.WhenAll(trackedTasks);

            sharedDownloads.CompleteAdding();

            if (!ct.IsCancellationRequested && loopCompleted)
            {
                UpdateBlogStats();
            }

            return Tuple.Create(highestId, apiLimitHit);
        }

        private void AddUrlsToDownloadList(XDocument document)
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
                foreach (var post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "photo" && (!tags.Any()) || post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        // photoset
                        if (post.Descendants("photoset").Any())
                        {
                            foreach (var photo in post.Descendants("photoset").Descendants("photo"))
                            {
                                string imageUrl = ParseImageUrl(photo);
                                if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                                {
                                    continue;
                                }

                                AddToDownloadList(Tuple.Create(PostTypes.Photo, imageUrl, post.Attribute("id").Value));
                            }
                        }
                        // single image
                        else
                        {
                            string imageUrl = ParseImageUrl(post);
                            if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                            {
                                continue;
                            }

                            AddToDownloadList(Tuple.Create(PostTypes.Photo, imageUrl, post.Attribute("id").Value));
                        }
                    }
                }
                // check for inline images
                foreach (var post in document.Descendants("post"))
                {
                    if (!tags.Any() || post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
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
                                AddToDownloadList(Tuple.Create(PostTypes.Photo, imageUrl, post.Attribute("id").Value));
                            }
                        }
                    }
                }
            }
            if (blog.DownloadVideo)
            {
                foreach (var post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "video" && (!tags.Any()) || post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        var videoUrl = post.Descendants("video-player").Where(x => x.Value.Contains("<source src=")).Select(result => Regex.Match(result.Value, "<source src=\"(.*)\" type=\"video/mp4\">").Groups[1].Value).First();

                        if (shellService.Settings.VideoSize == 1080)
                        {
                            AddToDownloadList(Tuple.Create(PostTypes.Video, videoUrl.Replace("/480", "") + ".mp4", post.Attribute("id").Value));
                        }
                        else if (shellService.Settings.VideoSize == 480)
                        {
                            AddToDownloadList(Tuple.Create(PostTypes.Video, "https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + "_480.mp4", post.Attribute("id").Value));
                        }
                    }
                }
            }
            if (blog.DownloadAudio)
            {
                foreach (var post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "audio" && (!tags.Any()) || post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        var audioUrl = post.Descendants("audio-player").Where(x => x.Value.Contains("src=")).Select(result => Regex.Match(result.Value, "src=\"(.*)\" height").Groups[1].Value).ToList();

                        foreach (string audiofile in audioUrl)
                        {
                            AddToDownloadList(Tuple.Create(PostTypes.Audio, WebUtility.UrlDecode(audiofile), post.Attribute("id").Value));
                        }
                    }
                }
            }
            if (blog.DownloadText)
            {
                foreach (var post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "regular" && (!tags.Any()) || post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        string textBody = ParseText(post);
                        AddToDownloadList(Tuple.Create(PostTypes.Text, textBody, post.Attribute("id").Value));
                    }
                }
            }
            if (blog.DownloadQuote)
            {
                foreach (var post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "quote" && (!tags.Any()) || post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        string textBody = ParseQuote(post);
                        AddToDownloadList(Tuple.Create(PostTypes.Quote, textBody, post.Attribute("id").Value));
                    }
                }
            }
            if (blog.DownloadLink)
            {
                foreach (var post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "link" && (!tags.Any()) || post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        string textBody = ParseLink(post);
                        AddToDownloadList(Tuple.Create(PostTypes.Link, textBody, post.Attribute("id").Value));
                    }
                }
            }
            if (blog.DownloadConversation)
            {
                foreach (var post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "conversation" && (!tags.Any()) || post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        string textBody = ParseConversation(post);
                        AddToDownloadList(Tuple.Create(PostTypes.Conversation, textBody, post.Attribute("id").Value));
                    }
                }
            }
            if (blog.CreatePhotoMeta)
            {
                foreach (var post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "photo" && (!tags.Any()) || post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        string textBody = ParsePhotoMeta(post);
                        AddToDownloadList(Tuple.Create(PostTypes.PhotoMeta, textBody, post.Attribute("id").Value));
                    }
                }
            }
            if (blog.CreateVideoMeta)
            {
                foreach (var post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "video" && (!tags.Any()) || post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        string textBody = ParseVideoMeta(post);
                        AddToDownloadList(Tuple.Create(PostTypes.VideoMeta, textBody, post.Attribute("id").Value));
                    }
                }
            }
            if (blog.CreateAudioMeta)
            {
                foreach (var post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "audio" && (!tags.Any()) || post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        string textBody = ParseAudioMeta(post);
                        AddToDownloadList(Tuple.Create(PostTypes.AudioMeta, textBody, post.Attribute("id").Value));
                    }
                }
            }
        }

        private void CountPostTypes(XContainer document)
        {
            Interlocked.Add(ref counter.Photos, document.Descendants("post").Count(post => post.Attribute("type").Value == "photo"));
            Interlocked.Add(ref counter.Photos, document.Descendants("photo").Count() - 1);
            Interlocked.Add(ref counter.Photos, document.Descendants("post").Count(post => post.Nodes().ToString().Contains("tumblr_inline")));
            Interlocked.Add(ref counter.Videos, document.Descendants("post").Count(post => post.Attribute("type").Value == "video"));
            Interlocked.Add(ref counter.Audios, document.Descendants("post").Count(post => post.Attribute("type").Value == "audio"));
            Interlocked.Add(ref counter.Texts, document.Descendants("post").Count(post => post.Attribute("type").Value == "regular"));
            Interlocked.Add(ref counter.Conversations, document.Descendants("post").Count(post => post.Attribute("type").Value == "conversation"));
            Interlocked.Add(ref counter.Quotes, document.Descendants("post").Count(post => post.Attribute("type").Value == "quote"));
            Interlocked.Add(ref counter.Links, document.Descendants("post").Count(post => post.Attribute("type").Value == "link"));
        }

        private void UpdateBlogStats()
        {
            blog.TotalCount = downloadList.Count;
            blog.Photos = counter.Photos;
            blog.Videos = counter.Videos;
            blog.Audios = counter.Audios;
            blog.Texts = counter.Texts;
            blog.Conversations = counter.Conversations;
            blog.Quotes = counter.Quotes;
            blog.NumberOfLinks = counter.Links;
            blog.PhotoMetas = counter.PhotoMetas;
            blog.VideoMetas = counter.VideoMetas;
            blog.AudioMetas = counter.AudioMetas;
        }

        private void AddToDownloadList(Tuple<PostTypes, string, string> addToList)
        {
            downloadList.Add(addToList);
            sharedDownloads.Add(addToList);
        }

        private string ParseImageUrl(XElement post)
        {
            var imageUrl = post.Elements("photo-url")
                .FirstOrDefault(photo_url => photo_url.Attribute("max-width")
                .Value == shellService.Settings.ImageSize.ToString()).Value;

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
    }
}
