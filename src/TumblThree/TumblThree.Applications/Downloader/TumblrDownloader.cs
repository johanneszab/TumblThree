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
using System.Windows.Media;
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
        private readonly IBlog blog;
        private readonly ICrawlerService crawlerService;
        private readonly IShellService shellService;

        public TumblrDownloader(IShellService shellService, ICrawlerService crawlerService, IBlog blog)
            : base(shellService, crawlerService, blog)
        {
            this.shellService = shellService;
            this.crawlerService = crawlerService;
            this.blog = blog;
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

        public async Task Crawl(IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            Logger.Verbose("TumblrDownloader.Crawl:Start");

            Task<Tuple<ulong, bool>> grabber = GetUrlsAsync(progress, ct, pt);
            Task<bool> downloader = DownloadBlogAsync(progress, ct, pt);
            Tuple<ulong, bool> grabberResult = await grabber;
            bool apiLimitHit = grabberResult.Item2;

            UpdateProgressQueueInformation(progress, Resources.ProgressUniqueDownloads);

            blog.DuplicatePhotos = DetermineDuplicates(PostTypes.Photo);
            blog.DuplicateVideos = DetermineDuplicates(PostTypes.Video);
            blog.DuplicateAudios = DetermineDuplicates(PostTypes.Audio);
            blog.TotalCount = (blog.TotalCount - blog.DuplicatePhotos - blog.DuplicateAudios - blog.DuplicateVideos);

            CleanCollectedBlogStatistics();

            bool finishedDownloading = await downloader;

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
            HttpWebRequest request = CreateWebReqeust(url);

            using (var response = await request.GetResponseAsync() as HttpWebResponse)
            {
                using (var stream = GetStreamForApiRequest(response.GetResponseStream()))
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
            string url = GetApiUrl(blog.Url, blog.PageSize, pageId * blog.PageSize);

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
                .Replace("_raw", "_" + shellService.Settings.ImageSize)
                .Replace("_1280", "_" + shellService.Settings.ImageSize)
                .Replace("_540", "_" + shellService.Settings.ImageSize)
                .Replace("_500", "_" + shellService.Settings.ImageSize)
                .Replace("_400", "_" + shellService.Settings.ImageSize)
                .Replace("_250", "_" + shellService.Settings.ImageSize)
                .Replace("_100", "_" + shellService.Settings.ImageSize)
                .Replace("_75sq", "_" + shellService.Settings.ImageSize)
                .ToString();
        }

        /// <returns>
        ///     Return the url without the size and type suffix (e.g.
        ///     https://68.media.tumblr.com/51a99943f4aa7068b6fd9a6b36e4961b/tumblr_mnj6m9Huml1qat3lvo1).
        /// </returns>
        protected override string GetCoreImageUrl(string url)
        {
            // return url.Split('_')[0] + "_" + url.Split('_')[1];
            return url;
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
            return statisticsBag.Where(url => url.PostType.Equals(type))
                                .GroupBy(url => url.Url)
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

        private IEnumerable<int> GetPageNumbers()
        {
            if (string.IsNullOrEmpty(blog.DownloadPages))
            {
                int totalPosts = blog.Posts;
                if (!TestRange(blog.PageSize, 1, 50))
                    blog.PageSize = 50;
                int totalPages = (totalPosts / blog.PageSize) + 1;

                return Enumerable.Range(0, totalPages);
            }
            return RangeToSequence(blog.DownloadPages);
        }

        private static bool TestRange(int numberToCheck, int bottom, int top)
        {
            return (numberToCheck >= bottom && numberToCheck <= top);
        }

        static IEnumerable<int> RangeToSequence(string input)
        {
            string[] parts = input.Split(',');
            foreach (string part in parts)
            {
                if (!part.Contains('-'))
                {
                    yield return int.Parse(part);
                    continue;
                }
                string[] rangeParts = part.Split('-');
                int start = int.Parse(rangeParts[0]);
                int end = int.Parse(rangeParts[1]);

                while (start <= end)
                {
                    yield return start;
                    start++;
                }
            }
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
                return 0;
            }
            if (!string.IsNullOrEmpty(blog.DownloadPages))
            {
                blog.ForceRescan = false;
                return 0;
            }
            return lastId;
        }

        private async Task<Tuple<ulong, bool>> GetUrlsAsync(IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
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

            foreach (int pageNumber in GetPageNumbers())
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

                        var tags = new List<string>();
                        if (!string.IsNullOrWhiteSpace(blog.Tags))
                        {
                            tags = blog.Tags.Split(',').Select(x => x.Trim()).ToList();
                        }

                        AddUrlsToDownloadList(document, tags);
                    }
                    catch (WebException webException)
                    {
                        if (webException.Message.Contains("429"))
                        {
                            // TODO: add retry logic?
                            apiLimitHit = true;
                            Logger.Error("TumblrDownloader:GetUrls:WebException {0}", webException);
                            shellService.ShowError(webException, Resources.LimitExceeded, blog.Name);
                        }
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }

                    numberOfPostsCrawled += blog.PageSize;
                    UpdateProgressQueueInformation(progress, Resources.ProgressGetUrlLong, numberOfPostsCrawled, totalPosts);
                })());
            }
            await Task.WhenAll(trackedTasks);

            producerConsumerCollection.CompleteAdding();

            if (!ct.IsCancellationRequested && completeGrab)
            {
                UpdateBlogStats();
            }

            return new Tuple<ulong, bool>(highestId, apiLimitHit);
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

        private void AddUrlsToDownloadList(XDocument document, IList<string> tags)
        {
            try
            {
                AddPhotoUrlToDownloadList(document, tags);
                AddVideoUrlToDownloadList(document, tags);
                AddAudioUrlToDownloadList(document, tags);
                AddTextUrlToDownloadList(document, tags);
                AddQuoteUrlToDownloadList(document, tags);
                AddLinkUrlToDownloadList(document, tags);
                AddConversationUrlToDownloadList(document, tags);
                AddAnswerUrlToDownloadList(document, tags);
                AddPhotoMetaUrlToDownloadList(document, tags);
                AddVideoMetaUrlToDownloadList(document, tags);
                AddAudioMetaUrlToDownloadList(document, tags);
            }
            catch (NullReferenceException)
            {

            }
        }

        private bool CheckIfDownloadRebloggedPosts(XElement post)
        {
            if (!blog.DownloadRebloggedPosts)
            {
                if (!post.Attributes("reblogged-from-url").Any())
                    return true;
                return false;
            }
            return true;
        }

        protected override async Task DownloadPhotoAsync(IProgress<DataModels.DownloadProgress> progress, TumblrPost downloadItem, CancellationToken ct)
        {
            string url = Url(downloadItem);
            foreach (string host in shellService.Settings.TumblrHosts)
            {
                url = BuildRawImageUrl(url, host);
                if (await DownloadDetectedImageUrl(progress, url, PostDate(downloadItem), ct))
                    return;
            }
            await DownloadDetectedImageUrl(progress, Url(downloadItem), PostDate(downloadItem), ct);
        }

        private async Task<bool> DownloadDetectedImageUrl(IProgress<DownloadProgress> progress, string url, DateTime postDate, CancellationToken ct)
        {
            if (!(CheckIfFileExistsInDB(url) || CheckIfBlogShouldCheckDirectory(GetCoreImageUrl(url))))
            {
                string blogDownloadLocation = blog.DownloadLocation();
                string fileName = url.Split('/').Last();
                string fileLocation = FileLocation(blogDownloadLocation, fileName);
                string fileLocationUrlList = FileLocationLocalized(blogDownloadLocation, Resources.FileNamePhotos);
                UpdateProgressQueueInformation(progress, Resources.ProgressDownloadImage, fileName);
                if (await DownloadBinaryFile(fileLocation, fileLocationUrlList, url, ct))
                {
                    SetFileDate(fileLocation, postDate);
                    UpdateBlogPostCount(ref counter.Photos, value => blog.DownloadedPhotos = value);
                    UpdateBlogProgress(ref counter.TotalDownloads);
                    UpdateBlogDB(fileName);
                    if (shellService.Settings.EnablePreview)
                    {
                        if (!fileName.EndsWith(".gif"))
                        {
                            blog.LastDownloadedPhoto = Path.GetFullPath(fileLocation);
                        }
                        else
                        {
                            blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Builds a tumblr raw image url from any sized tumblr image url if the ImageSize is set to "raw".
        /// </summary>
        /// <param name="url">The url detected from the crawler.</param>
        /// <param name="host">Hostname to insert in the original url.</param>
        /// <returns></returns>
        public string BuildRawImageUrl(string url, string host)
        {
            if (shellService.Settings.ImageSize == "raw")
            {
                string path = new Uri(url).LocalPath.TrimStart('/');
                var imageDimension = new Regex("_\\d+");
                path = imageDimension.Replace(path, "_raw");
                return "https://" + host + "/" + path;
            }
            return url;
        }

        private void AddPhotoUrlToDownloadList(XDocument document, IList<string> tags)
        {
            if (blog.DownloadPhoto)
            {
                foreach (XElement post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "photo" && (!tags.Any() ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase))))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            AddPhotoUrl(post);
                            AddPhotoSetUrl(post);
                        }
                    }
                }

                // check for inline images
                foreach (XElement post in document.Descendants("post").Where(p => p.Attribute("type").Value != "photo"))
                {
                    if (!tags.Any() || post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
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
                    if (post.Attribute("type").Value == "video" && (!tags.Any() ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase))))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                            AddVideoUrl(post);
                    }
                }

                // check for inline images
                foreach (XElement post in document.Descendants("post").Where(p => p.Attribute("type").Value != "video"))
                {
                    if (!tags.Any() || post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                            AddInlineVideoUrl(post);
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
                    if (post.Attribute("type").Value == "audio" && (!tags.Any() ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase))))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
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
                    if (post.Attribute("type").Value == "regular" && (!tags.Any() ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase))))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string textBody = ParseText(post);
                            AddToDownloadList(new TumblrPost(PostTypes.Text, textBody, post.Attribute("id").Value));
                        }
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
                    if (post.Attribute("type").Value == "quote" && (!tags.Any() ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase))))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string textBody = ParseQuote(post);
                            AddToDownloadList(new TumblrPost(PostTypes.Quote, textBody, post.Attribute("id").Value));
                        }
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
                    if (post.Attribute("type").Value == "link" && (!tags.Any() ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase))))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string textBody = ParseLink(post);
                            AddToDownloadList(new TumblrPost(PostTypes.Link, textBody, post.Attribute("id").Value));
                        }
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
                    if (post.Attribute("type").Value == "conversation" && (!tags.Any() ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase))))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string textBody = ParseConversation(post);
                            AddToDownloadList(new TumblrPost(PostTypes.Conversation, textBody, post.Attribute("id").Value));
                        }
                    }
                }
            }
        }

        private void AddAnswerUrlToDownloadList(XDocument document, IList<string> tags)
        {
            if (blog.DownloadAnswer)
            {
                foreach (XElement post in document.Descendants("post"))
                {
                    if (post.Attribute("type").Value == "answer" && (!tags.Any() ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase))))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string textBody = ParseAnswer(post);
                            AddToDownloadList(new TumblrPost(PostTypes.Answer, textBody, post.Attribute("id").Value));
                        }
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
                    if (post.Attribute("type").Value == "photo" && (!tags.Any() ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase))))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string textBody = ParsePhotoMeta(post);
                            AddToDownloadList(new TumblrPost(PostTypes.PhotoMeta, textBody, post.Attribute("id").Value));
                        }
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
                    if (post.Attribute("type").Value == "video" && (!tags.Any() ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase))))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string textBody = ParseVideoMeta(post);
                            AddToDownloadList(new TumblrPost(PostTypes.VideoMeta, textBody, post.Attribute("id").Value));
                        }
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
                    if (post.Attribute("type").Value == "audio" && (!tags.Any() ||
                        post.Descendants("tag").Any(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase))))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string textBody = ParseAudioMeta(post);
                            AddToDownloadList(new TumblrPost(PostTypes.AudioMeta, textBody, post.Attribute("id").Value));
                        }
                    }
                }
            }
        }

        private void UpdateBlogStats()
        {
            blog.TotalCount = statisticsBag.Count;
            blog.Photos = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Photo));
            blog.Videos = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Video));
            blog.Audios = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Audio));
            blog.Texts = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Text));
            blog.Answers = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Answer));
            blog.Conversations = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Conversation));
            blog.Quotes = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Quote));
            blog.NumberOfLinks = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Link));
            blog.PhotoMetas = statisticsBag.Count(url => url.PostType.Equals(PostTypes.PhotoMeta));
            blog.VideoMetas = statisticsBag.Count(url => url.PostType.Equals(PostTypes.VideoMeta));
            blog.AudioMetas = statisticsBag.Count(url => url.PostType.Equals(PostTypes.AudioMeta));
        }

        private void AddToDownloadList(TumblrPost addToList)
        {
            producerConsumerCollection.Add(addToList);
            statisticsBag.Add(addToList);
        }

        private string ImageSize()
        {
            if (shellService.Settings.ImageSize.ToString() == "raw")
                return "1280";
            return shellService.Settings.ImageSize.ToString();
        }

        private string ParseImageUrl(XContainer post)
        {
            string imageUrl = post.Elements("photo-url")
                      .FirstOrDefault(photo_url => photo_url.Attribute("max-width")
                      .Value == ImageSize()).Value;

            if (blog.ForceSize)
            {
                imageUrl = ResizeTumblrImageUrl(imageUrl);
            }

            return imageUrl;
        }

        private void AddInlinePhotoUrl(XElement post)
        {
            var regex = new Regex("\"(http[\\S]*media.tumblr.com[\\S]*(jpg|png|gif))\"");
            foreach (Match match in regex.Matches(post.Value))
            {
                string imageUrl = match.Groups[1].Value;
                if (imageUrl.Contains("avatar") || imageUrl.Contains("previews"))
                    continue;
                if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                {
                    continue;
                }
                imageUrl = ResizeTumblrImageUrl(imageUrl);
                AddToDownloadList(new TumblrPost(PostTypes.Photo, imageUrl, post.Attribute("id").Value, post.Attribute("unix-timestamp").Value));
            }
        }

        private void AddInlineVideoUrl(XElement post)
        {
            var regex = new Regex("\"(http[\\S]*.com/video_file/[\\S]*)\"");
            foreach (Match match in regex.Matches(post.Value))
            {
                string videoUrl = match.Groups[1].Value;
                if (shellService.Settings.VideoSize == 1080)
                {
                    AddToDownloadList(new TumblrPost(PostTypes.Video, videoUrl.Replace("/480", "") + ".mp4", post.Attribute("id").Value, post.Attribute("unix-timestamp").Value));
                }
                else if (shellService.Settings.VideoSize == 480)
                {
                    AddToDownloadList(new TumblrPost(PostTypes.Video,
                        "https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + "_480.mp4",
                        post.Attribute("id").Value, post.Attribute("unix-timestamp").Value));
                }
            }
        }

        private void AddPhotoUrl(XElement post)
        {
            string imageUrl = ParseImageUrl(post);
            if (blog.SkipGif && imageUrl.EndsWith(".gif"))
            {
                return;
            }
            AddToDownloadList(new TumblrPost(PostTypes.Photo, imageUrl, post.Attribute("id").Value, post.Attribute("unix-timestamp").Value));
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
                AddToDownloadList(new TumblrPost(PostTypes.Photo, imageUrl, post.Attribute("id").Value, post.Attribute("unix-timestamp").Value));
            }
        }

        private void AddVideoUrl(XElement post)
        {
            string videoUrl = post.Descendants("video-player")
                                  .Select(result => Regex.Match(result.Value, "<source src=\"([\\S]*)\"")
                                                         .Groups[1].Value)
                                  .FirstOrDefault();

            if (shellService.Settings.VideoSize == 1080)
            {

                AddToDownloadList(new TumblrPost(PostTypes.Video,
                    "https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + ".mp4",
                    post.Attribute("id").Value, post.Attribute("unix-timestamp").Value));
            }
            else if (shellService.Settings.VideoSize == 480)
            {

                AddToDownloadList(new TumblrPost(PostTypes.Video,
                    "https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + "_480.mp4",
                    post.Attribute("id").Value, post.Attribute("unix-timestamp").Value));
            }
        }

        private void AddAudioUrl(XElement post)
        {
            string audioUrl = post.Descendants("audio-embed")
                                  .Select(result => Regex.Match(result.Value, "src=\"([\\S]*)\"")
                                                         .Groups[1].Value)
                                  .FirstOrDefault();

            AddToDownloadList(new TumblrPost(PostTypes.Audio, WebUtility.UrlDecode(audioUrl), post.Attribute("id").Value, post.Attribute("unix-timestamp").Value));
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
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) +
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
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) +
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
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) +
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
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) +
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
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) +
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
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) +
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
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Title, post.Element("regular-title")?.Value) +
                   Environment.NewLine + post.Element("regular-body")?.Value +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseAnswer(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.Attribute("reblogged-from-url")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.Attribute("reblogged-from-name")?.Value) +
                   Environment.NewLine +
                   post.Element("question")?.Value +
                   Environment.NewLine +
                   post.Element("answer")?.Value +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }
    }
}
