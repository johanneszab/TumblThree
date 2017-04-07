using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Downloader
{
    [Export(typeof(IDownloader))]
    [ExportMetadata("BlogType", BlogTypes.tumblr)]
    public class TumblrDownloader : Downloader, IDownloader
    {
        private readonly TumblrBlog blog;
        private readonly ICrawlerService crawlerService;

        private readonly IShellService shellService;

        public TumblrDownloader(IShellService shellService, ICrawlerService crawlerService, IBlog blog)
            : base(shellService, crawlerService, blog)
        {
            this.shellService = shellService;
            this.crawlerService = crawlerService;
            this.blog = (TumblrBlog)blog;
        }

        public new async Task IsBlogOnlineAsync()
        {
            try
            {
                await GetApiPageAsync(1);
                blog.Online = true;
            }
            catch (WebException)
            {
                blog.Online = false;
            }
        }

        public override async Task UpdateMetaInformationAsync()
        {
            if (blog.Online)
            {
                XDocument document = await GetApiPageAsync(1);

                if (document.Root.Descendants().Any())
                {
                    blog.Title = document.Element("tumblr").Element("tumblelog").Attribute("title")?.Value;
                    blog.Description = document.Element("tumblr").Element("tumblelog")?.Value;
                    blog.TotalCount = int.Parse(document.Element("tumblr").Element("posts").Attribute("total")?.Value);
                }
            }
        }

        public void Crawl(IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            Logger.Verbose("TumblrDownloader.Crawl:Start");

            Task<Tuple<ulong, bool>> grabber = Task.Run(() => GetUrls(progress, ct, pt));
            Task<bool> downloader = Task.Run(() => DownloadBlog(progress, ct, pt));
            Tuple<ulong, bool> grabberResult = grabber.Result;
            bool apiLimitHit = grabberResult.Item2;

            UpdateProgressQueueInformation(progress, Resources.ProgressUniqueDownloads);

            blog.DuplicatePhotos = DetermineDuplicates(PostTypes.Photo);
            blog.DuplicateVideos = DetermineDuplicates(PostTypes.Video);
            blog.DuplicateAudios = DetermineDuplicates(PostTypes.Audio);
            blog.TotalCount = (blog.TotalCount - blog.DuplicatePhotos - blog.DuplicateAudios - blog.DuplicateVideos);

            bool finishedDownloading = downloader.Result;

            if (!ct.IsCancellationRequested)
            {
                blog.LastCompleteCrawl = DateTime.Now;
                if (finishedDownloading && !apiLimitHit)
                {
                    blog.LastId = grabberResult.Item1;
                }
            }

            blog.Save();

            UpdateProgressQueueInformation(progress, "");
        }

        private new async Task<XDocument> RequestDataAsync(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ProtocolVersion = HttpVersion.Version11;
            request.UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
            request.AllowAutoRedirect = true;
            request.KeepAlive = true;
            request.Pipelined = true;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ReadWriteTimeout = shellService.Settings.TimeOut * 1000;
            request.Timeout = -1;
            ServicePointManager.DefaultConnectionLimit = 400;
            if (!string.IsNullOrEmpty(shellService.Settings.ProxyHost))
            {
                request.Proxy = new WebProxy(shellService.Settings.ProxyHost, int.Parse(shellService.Settings.ProxyPort));
            }
            else
            {
                request.Proxy = null;
            }

            var bandwidth = 2000000;
            if (shellService.Settings.LimitScanBandwidth)
            {
                bandwidth = shellService.Settings.Bandwidth;
            }

            using (var response = await request.GetResponseAsync() as HttpWebResponse)
            {
                using (
                    var stream = new ThrottledStream(response.GetResponseStream(),
                        (bandwidth / shellService.Settings.ParallelImages) * 1024))
                {
                    using (var buffer = new BufferedStream(stream))
                    {
                        using (var reader = new StreamReader(buffer))
                        {
                            return XDocument.Load(reader);
                        }
                    }
                }
            }
        }

        private string GetApiUrl(string url, int count, int start = 0)
        {
            if (url.Last<char>() != '/')
            {
                url += "/api/read";
            }
            else
            {
                url += "api/read";
            }

            var parameters = new Dictionary<string, string>
            {
                { "num", count.ToString() }
            };
            if (start > 0)
            {
                parameters["start"] = start.ToString();
            }
            return url + "?" + UrlEncode(parameters);
        }

        private async Task<XDocument> GetApiPageAsync(int pageId)
        {
            string url = GetApiUrl(blog.Url, 50, pageId * 50);

            if (shellService.Settings.LimitConnections)
            {
                crawlerService.Timeconstraint.Acquire();
                return await RequestDataAsync(url);
            }
            return await RequestDataAsync(url);
        }

        private string ResizeTumblrImageUrl(string imageUrl)
        {
            var sb = new StringBuilder(imageUrl);
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
        ///     Return the url without the size and type suffix (e.g.
        ///     https://68.media.tumblr.com/51a99943f4aa7068b6fd9a6b36e4961b/tumblr_mnj6m9Huml1qat3lvo1).
        /// </returns>
        protected override string GetCoreImageUrl(string url)
        {
            return url.Split('_')[0] + "_" + url.Split('_')[1];
        }

        protected override bool CheckIfFileExistsInDirectory(string url)
        {
            string fileName = url.Split('/').Last();
            Monitor.Enter(lockObjectDirectory);
            string blogPath = blog.DownloadLocation();
            if (Directory.EnumerateFiles(blogPath).Any(file => file.Contains(fileName)))
            {
                Monitor.Exit(lockObjectDirectory);
                return true;
            }
            Monitor.Exit(lockObjectDirectory);
            return false;
        }

        private int DetermineDuplicates(PostTypes type)
        {
            return statisticsBag.Where(url => url.Item1.Equals(type))
                                .GroupBy(url => url.Item2)
                                .Where(g => g.Count() > 1)
                                .Sum(g => g.Count() - 1);
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

        private ulong GetLastPostId()
        {
            ulong lastId = blog.LastId;
            if (blog.ForceRescan)
            {
                blog.ForceRescan = false;
                lastId = 0;
            }
            return lastId;
        }

        private async Task<Tuple<ulong, bool>> GetUrls(IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            var semaphoreSlim = new SemaphoreSlim(shellService.Settings.ParallelScans);
            var trackedTasks = new List<Task>();
            var numberOfPostsCrawled = 0;
            var apiLimitHit = false;
            var completeGrab = true;

            ulong lastId = GetLastPostId();

            await UpdateTotalPostCount();
            int totalPosts = blog.Posts;

            ulong highestId = await GetHighestPostId();

            // The Tumblr api v1 shows 50 posts at max, determine the number of pages to crawl
            int totalPages = (totalPosts / 50) + 1;

            foreach (int pageNumber in Enumerable.Range(0, totalPages))
            {
                await semaphoreSlim.WaitAsync();

                if (!completeGrab)
                {
                    break;
                }

                if (ct.IsCancellationRequested)
                {
                    break;
                }
                if (pt.IsPaused)
                {
                    pt.WaitWhilePausedWithResponseAsyc().Wait();
                }

                trackedTasks.Add(new Func<Task>(async () =>
                {
                    try
                    {
                        XDocument document = await GetApiPageAsync(pageNumber);

                        completeGrab = CheckPostAge(document, lastId);

                        AddUrlsToDownloadList(document);
                    }
                    catch (WebException webException)
                    {
                        if (webException.Message.Contains("429"))
                        {
                            // add retry logic?
                            apiLimitHit = true;
                            Logger.Error("TumblrDownloader:GetUrls:WebException {0}", webException);
                            shellService.ShowError(webException, Resources.LimitExceeded, blog.Name);
                        }
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }

                    numberOfPostsCrawled += 50;
                    UpdateProgressQueueInformation(progress, Resources.ProgressGetUrl, numberOfPostsCrawled, totalPosts);
                })());
            }
            await Task.WhenAll(trackedTasks);

            producerConsumerCollection.CompleteAdding();

            if (!ct.IsCancellationRequested && completeGrab)
            {
                UpdateBlogStats();
            }

            return Tuple.Create(highestId, apiLimitHit);
        }

        private static bool CheckPostAge(XDocument document, ulong lastId)
        {
            ulong highestPostId = 0;
            ulong.TryParse(document.Element("tumblr").Element("posts").Element("post")?.Attribute("id").Value,
                out highestPostId);

            if (highestPostId < lastId)
            {
                return false;
            }
            return true;
        }

        private void AddUrlsToDownloadList(XDocument document)
        {
            var tags = new List<string>();
            if (!string.IsNullOrWhiteSpace(blog.Tags))
            {
                tags = blog.Tags.Split(',').Select(x => x.Trim()).ToList();
            }

            try
            {
                AddPhotoUrlToDownloadList(document, tags);
                AddVideoUrlToDownloadList(document, tags);
                AddAudioUrlToDownloadList(document, tags);
                AddTextUrlToDownloadList(document, tags);
                AddQuoteUrlToDownloadList(document, tags);
                AddLinkUrlToDownloadList(document, tags);
                AddConversationUrlToDownloadList(document, tags);
                AddPhotoMetaUrlToDownloadList(document, tags);
                AddVideoMetaUrlToDownloadList(document, tags);
                AddAudioMetaUrlToDownloadList(document, tags);
            }
            catch (NullReferenceException)
            {
                
            }
        }

        private void AddPhotoUrlToDownloadList(XDocument document, IList<string> tags)
        {
            if (blog.DownloadPhoto)
            {
                foreach (XElement post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "photo" && (!tags.Any()) ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        AddPhotoUrl(post);
                        AddPhotoSetUrl(post);
                    }
                }

                // check for inline images
                foreach (XElement post in document.Descendants("post"))
                {
                    if (!tags.Any() || post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        AddInlinePhotoUrl(post);
                    }
                }
            }
        }

        private void AddVideoUrlToDownloadList(XDocument document, IList<string> tags)
        {
            if (blog.DownloadVideo)
            {
                foreach (XElement post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "video" && (!tags.Any()) ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        AddVideoUrl(post);
                    }
                }
            }
        }

        private void AddAudioUrlToDownloadList(XDocument document, IList<string> tags)
        {
            if (blog.DownloadAudio)
            {
                foreach (XElement post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "audio" && (!tags.Any()) ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        AddAudioUrl(post);
                    }
                }
            }
        }

        private void AddTextUrlToDownloadList(XDocument document, IList<string> tags)
        {
            if (blog.DownloadText)
            {
                foreach (XElement post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "regular" && (!tags.Any()) ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        string textBody = ParseText(post);
                        AddToDownloadList(Tuple.Create(PostTypes.Text, textBody, post.Attribute("id").Value));
                    }
                }
            }
        }

        private void AddQuoteUrlToDownloadList(XDocument document, IList<string> tags)
        {
            if (blog.DownloadQuote)
            {
                foreach (XElement post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "quote" && (!tags.Any()) ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        string textBody = ParseQuote(post);
                        AddToDownloadList(Tuple.Create(PostTypes.Quote, textBody, post.Attribute("id").Value));
                    }
                }
            }
        }

        private void AddLinkUrlToDownloadList(XDocument document, IList<string> tags)
        {
            if (blog.DownloadLink)
            {
                foreach (XElement post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "link" && (!tags.Any()) ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        string textBody = ParseLink(post);
                        AddToDownloadList(Tuple.Create(PostTypes.Link, textBody, post.Attribute("id").Value));
                    }
                }
            }
        }

        private void AddConversationUrlToDownloadList(XDocument document, IList<string> tags)
        {
            if (blog.DownloadConversation)
            {
                foreach (XElement post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "conversation" && (!tags.Any()) ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        string textBody = ParseConversation(post);
                        AddToDownloadList(Tuple.Create(PostTypes.Conversation, textBody, post.Attribute("id").Value));
                    }
                }
            }
        }

        private void AddPhotoMetaUrlToDownloadList(XDocument document, IList<string> tags)
        {
            if (blog.CreatePhotoMeta)
            {
                foreach (XElement post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "photo" && (!tags.Any()) ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        string textBody = ParsePhotoMeta(post);
                        AddToDownloadList(Tuple.Create(PostTypes.PhotoMeta, textBody, post.Attribute("id").Value));
                    }
                }
            }
        }

        private void AddVideoMetaUrlToDownloadList(XDocument document, IList<string> tags)
        {
            if (blog.CreateVideoMeta)
            {
                foreach (XElement post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "video" && (!tags.Any()) ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        string textBody = ParseVideoMeta(post);
                        AddToDownloadList(Tuple.Create(PostTypes.VideoMeta, textBody, post.Attribute("id").Value));
                    }
                }
            }
        }

        private void AddAudioMetaUrlToDownloadList(XDocument document, IList<string> tags)
        {
            if (blog.CreateAudioMeta)
            {
                foreach (XElement post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "audio" && (!tags.Any()) ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        string textBody = ParseAudioMeta(post);
                        AddToDownloadList(Tuple.Create(PostTypes.AudioMeta, textBody, post.Attribute("id").Value));
                    }
                }
            }
        }

        private void UpdateBlogStats()
        {
            blog.TotalCount = statisticsBag.Count;
            blog.Photos = statisticsBag.Count(url => url.Item1.Equals(PostTypes.Photo));
            blog.Videos = statisticsBag.Count(url => url.Item1.Equals(PostTypes.Video));
            blog.Audios = statisticsBag.Count(url => url.Item1.Equals(PostTypes.Audio));
            blog.Texts = statisticsBag.Count(url => url.Item1.Equals(PostTypes.Text));
            blog.Conversations = statisticsBag.Count(url => url.Item1.Equals(PostTypes.Conversation));
            blog.Quotes = statisticsBag.Count(url => url.Item1.Equals(PostTypes.Quote));
            blog.NumberOfLinks = statisticsBag.Count(url => url.Item1.Equals(PostTypes.Link));
            blog.PhotoMetas = statisticsBag.Count(url => url.Item1.Equals(PostTypes.PhotoMeta));
            blog.VideoMetas = statisticsBag.Count(url => url.Item1.Equals(PostTypes.VideoMeta));
            blog.AudioMetas = statisticsBag.Count(url => url.Item1.Equals(PostTypes.AudioMeta));
        }

        private void AddToDownloadList(Tuple<PostTypes, string, string> addToList)
        {
            statisticsBag.Add(addToList);
            producerConsumerCollection.Add(addToList);
        }

        private string ParseImageUrl(XContainer post)
        {
            string imageUrl = post.Elements("photo-url")
                                  .FirstOrDefault(photo_url => photo_url.Attribute("max-width")
                                                                        .Value == shellService.Settings.ImageSize.ToString()).Value;

            if (blog.ForceSize)
            {
                imageUrl = ResizeTumblrImageUrl(imageUrl);
            }

            return imageUrl;
        }

        private void AddInlinePhotoUrl(XElement post)
        {
            if (!string.Concat(post.Nodes()).Contains("tumblr_inline"))
            {
                return;
            }
            var regex = new Regex("<img src=\"(.*?)\"");
            foreach (Match match in regex.Matches(post.Value))
            {
                string imageUrl = match.Groups[1].Value;
                if (blog.ForceSize)
                {
                    imageUrl = ResizeTumblrImageUrl(imageUrl);
                }
                AddToDownloadList(Tuple.Create(PostTypes.Photo, imageUrl, post.Attribute("id").Value));
            }
        }

        private void AddPhotoUrl(XElement post)
        {
            string imageUrl = ParseImageUrl(post);
            if (blog.SkipGif && imageUrl.EndsWith(".gif"))
            {
                return;
            }
            AddToDownloadList(Tuple.Create(PostTypes.Photo, imageUrl, post.Attribute("id").Value));
        }

        private void AddPhotoSetUrl(XElement post)
        {
            if (!post.Descendants("photoset").Any())
            {
                return;
            }
            foreach (string imageUrl in post.Descendants("photoset")
                                            .Descendants("photo")
                                            .Select(ParseImageUrl)
                                            .Where(imageUrl => !blog.SkipGif || !imageUrl.EndsWith(".gif")))
            {
                AddToDownloadList(Tuple.Create(PostTypes.Photo, imageUrl, post.Attribute("id").Value));
            }
        }

        private void AddVideoUrl(XElement post)
        {
            string videoUrl = post.Descendants("video-player")
                                  .Where(x => x.Value.Contains("<source src="))
                                  .Select(result => Regex.Match(result.Value, "<source src=\"(.*)\" type=\"video/mp4\">")
                                                         .Groups[1].Value)
                                  .FirstOrDefault();

            if (shellService.Settings.VideoSize == 1080)
            {

                AddToDownloadList(Tuple.Create(PostTypes.Video, videoUrl.Replace("/480", "") + ".mp4", post.Attribute("id").Value));
            }
            else if (shellService.Settings.VideoSize == 480)
            {

                AddToDownloadList(Tuple.Create(PostTypes.Video,
                    "https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + "_480.mp4",
                    post.Attribute("id").Value));
            }
        }

        private void AddAudioUrl(XElement post)
        {
            string audioUrl = post.Descendants("audio-player")
                                  .Where(x => x.Value.Contains("src="))
                                  .Select(result => Regex.Match(result.Value, "src=\"(.*)\" height")
                                                         .Groups[1].Value)
                                  .FirstOrDefault();

            AddToDownloadList(Tuple.Create(PostTypes.Audio, WebUtility.UrlDecode(audioUrl), post.Attribute("id").Value));
        }

        private static string ParsePhotoMeta(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoUrl, post.Element("photo-url").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoCaption, post.Element("photo-caption")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseVideoMeta(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.VideoPlayer, post.Element("video-player")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseAudioMeta(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.AudioCaption, post.Element("audio-caption")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Artist, post.Element("id3-artist")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Title, post.Element("id3-title")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.Element("id3-track")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Album, post.Element("id3-album")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Year, post.Element("id3-year")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseConversation(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Conversation, post.Element("conversation-text")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseLink(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Link, post.Element("link-text")?.Value) +
                   Environment.NewLine + post.Element("link-url")?.Value +
                   Environment.NewLine + post.Element("link-description")?.Value +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseQuote(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.Element("quote-text")?.Value) +
                   Environment.NewLine + post.Element("quote-source")?.Value +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseText(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Title, post.Element("regular-title")?.Value) +
                   Environment.NewLine + post.Element("regular-body")?.Value +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }
    }
}
