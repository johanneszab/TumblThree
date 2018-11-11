using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrCrawlerData;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.DataModels.TumblrSvcJson;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Parser;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(TumblrHiddenBlog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TumblrHiddenCrawler : AbstractTumblrCrawler, ICrawler
    {
        private readonly IDownloader downloader;
        private readonly ITumblrToTextParser<Post> tumblrJsonParser;
        private readonly IPostQueue<TumblrCrawlerData<Post>> jsonQueue;
        private readonly ICrawlerDataDownloader crawlerDataDownloader;

        private string tumblrKey = string.Empty;

        private bool incompleteCrawl = false;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        public TumblrHiddenCrawler(IShellService shellService, CancellationToken ct, PauseToken pt,
            IProgress<DownloadProgress> progress, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IDownloader downloader, ICrawlerDataDownloader crawlerDataDownloader,
            ITumblrToTextParser<Post> tumblrJsonParser, ITumblrParser tumblrParser, IImgurParser imgurParser,
            IGfycatParser gfycatParser, IWebmshareParser webmshareParser, IMixtapeParser mixtapeParser, IUguuParser uguuParser,
            ISafeMoeParser safemoeParser, ILoliSafeParser lolisafeParser, ICatBoxParser catboxParser,
            IPostQueue<TumblrPost> postQueue, IPostQueue<TumblrCrawlerData<Post>> jsonQueue, IBlog blog)
            : base(shellService, crawlerService, ct, pt, progress, webRequestFactory, cookieService, tumblrParser, imgurParser,
                gfycatParser, webmshareParser, mixtapeParser, uguuParser, safemoeParser, lolisafeParser, catboxParser, postQueue,
                blog)
        {
            this.downloader = downloader;
            this.tumblrJsonParser = tumblrJsonParser;
            this.jsonQueue = jsonQueue;
            this.crawlerDataDownloader = crawlerDataDownloader;
        }

        public override async Task IsBlogOnlineAsync()
        {
            try
            {
                tumblrKey = await UpdateTumblrKey("https://www.tumblr.com/dashboard/blog/" + blog.Name);
                string document = await GetSvcPageAsync("1", "0");
                blog.Online = true;
            }
            catch (WebException webException) when ((webException.Response != null))
            {
                if (HandleServiceUnavailableWebException(webException))
                    blog.Online = true;

                if (HandleNotFoundWebException(webException))
                    blog.Online = false;

                if (HandleLimitExceededWebException(webException))
                    blog.Online = true;
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.OnlineChecking);
                blog.Online = false;
            }
        }

        public override async Task UpdateMetaInformationAsync()
        {
            if (!blog.Online)
                return;

            try
            {
                tumblrKey = await UpdateTumblrKey("https://www.tumblr.com/dashboard/blog/" + blog.Name);
                string document = await GetSvcPageAsync("1", "0");
                var response = ConvertJsonToClass<TumblrJson>(document);

                if (response.meta.status == 200)
                {
                    blog.Title = response.response.posts.FirstOrDefault().blog.title;
                    blog.Description = response.response.posts.FirstOrDefault().blog.description;
                }
            }
            catch (WebException webException) when ((webException.Response != null))
            {
                HandleServiceUnavailableWebException(webException);
            }
        }

        public async Task CrawlAsync()
        {
            Logger.Verbose("TumblrHiddenCrawler.Crawl:Start");

            Task<Tuple<ulong, bool>> grabber = GetUrlsAsync();
            Task<bool> download = downloader.DownloadBlogAsync();

            Task crawlerDownloader = Task.CompletedTask;
            if (blog.DumpCrawlerData)
                crawlerDownloader = crawlerDataDownloader.DownloadCrawlerDataAsync();

            Tuple<ulong, bool> grabberResult = await grabber;
            bool apiLimitHit = grabberResult.Item2;

            UpdateProgressQueueInformation(Resources.ProgressUniqueDownloads);
            blog.DuplicatePhotos = DetermineDuplicates<PhotoPost>();
            blog.DuplicateVideos = DetermineDuplicates<VideoPost>();
            blog.DuplicateAudios = DetermineDuplicates<AudioPost>();
            blog.TotalCount = (blog.TotalCount - blog.DuplicatePhotos - blog.DuplicateAudios - blog.DuplicateVideos);

            CleanCollectedBlogStatistics();

            await crawlerDownloader;
            bool finishedDownloading = await download;

            if (!ct.IsCancellationRequested)
            {
                blog.LastCompleteCrawl = DateTime.Now;
                if (finishedDownloading && !apiLimitHit)
                {
                    blog.LastId = grabberResult.Item1;
                }
            }

            blog.Save();

            UpdateProgressQueueInformation("");
        }

        protected override IEnumerable<int> GetPageNumbers()
        {
            if (!TestRange(blog.PageSize, 1, 100))
                blog.PageSize = 100;

            return string.IsNullOrEmpty(blog.DownloadPages)
                ? Enumerable.Range(0, shellService.Settings.ConcurrentScans)
                : RangeToSequence(blog.DownloadPages);
        }

        private async Task<Tuple<ulong, bool>> GetUrlsAsync()
        {
            semaphoreSlim = new SemaphoreSlim(shellService.Settings.ConcurrentScans);
            trackedTasks = new List<Task>();
            ulong highestId = 0;

            GenerateTags();

            if (!await CheckIfLoggedInAsync())
            {
                Logger.Error("TumblrHiddenCrawler:GetUrlsAsync: {0}", "User not logged in");
                shellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, blog.Name);
                postQueue.CompleteAdding();
                incompleteCrawl = true;
                return new Tuple<ulong, bool>(highestId, incompleteCrawl);
            }

            highestId = await GetHighestPostIdAsync();

            foreach (int pageNumber in GetPageNumbers())
            {
                await semaphoreSlim.WaitAsync();
                trackedTasks.Add(CrawlPage(pageNumber));
            }

            await Task.WhenAll(trackedTasks);

            jsonQueue.CompleteAdding();
            postQueue.CompleteAdding();

            UpdateBlogStats();

            return new Tuple<ulong, bool>(highestId, incompleteCrawl);
        }

        private async Task CrawlPage(int pageNumber)
        {
            try
            {
                string document = await GetSvcPageAsync(blog.PageSize.ToString(), (blog.PageSize * pageNumber).ToString());
                var response = ConvertJsonToClass<TumblrJson>(document);
                await AddUrlsToDownloadList(response, pageNumber);
            }
            catch (WebException webException) when ((webException.Response != null))
            {
                if (HandleLimitExceededWebException(webException))
                    incompleteCrawl = true;
            }
            catch (TimeoutException timeoutException)
            {
                incompleteCrawl = true;
                HandleTimeoutException(timeoutException, Resources.Crawling);
            }
            catch
            {
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private async Task<ulong> GetHighestPostIdAsync()
        {
            try
            {
                return await GetHighestPostId();
            }
            catch (WebException webException)
            {
                HandleLimitExceededWebException(webException);
                return 0;
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
                return 0;
            }
        }

        private async Task<ulong> GetHighestPostId()
        {
            string document = await GetSvcPageAsync("1", "0");
            var response = ConvertJsonToClass<TumblrJson>(document);

            ulong highestId;
            ulong.TryParse(blog.Title = response.response.posts.FirstOrDefault().id, out highestId);
            return highestId;
        }

        private bool PostWithinTimeSpan(Post post)
        {
            if (string.IsNullOrEmpty(blog.DownloadFrom) && string.IsNullOrEmpty(blog.DownloadTo))
                return true;

            long downloadFromUnixTime = 0;
            long downloadToUnixTime = long.MaxValue;
            if (!string.IsNullOrEmpty(blog.DownloadFrom))
            {
                DateTime downloadFrom = DateTime.ParseExact(blog.DownloadFrom, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None);
                downloadFromUnixTime = new DateTimeOffset(downloadFrom).ToUnixTimeSeconds();
            }

            if (!string.IsNullOrEmpty(blog.DownloadTo))
            {
                DateTime downloadTo = DateTime.ParseExact(blog.DownloadTo, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None);
                downloadToUnixTime = new DateTimeOffset(downloadTo).ToUnixTimeSeconds();
            }

            long postTime = 0;
            postTime = Convert.ToInt64(post.timestamp);
            return downloadFromUnixTime < postTime && postTime < downloadToUnixTime;
        }

        private async Task<bool> CheckIfLoggedInAsync()
        {
            try
            {
                string document = await GetSvcPageAsync(blog.PageSize.ToString(), (blog.PageSize * 1).ToString());
            }
            catch (WebException webException) when ((webException.Response != null))
            {
                if (HandleServiceUnavailableWebException(webException))
                    return false;
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
                return false;
            }

            return true;
        }

        private async Task<string> GetSvcPageAsync(string limit, string offset)
        {
            //if (shellService.Settings.LimitConnections)
            //    crawlerService.Timeconstraint.Acquire();

            return await RequestDataAsync(limit, offset);
        }

        protected virtual async Task<string> RequestDataAsync(string limit, string offset)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                string url = @"https://www.tumblr.com/svc/indash_blog?tumblelog_name_or_id=" + blog.Name +
                             @"&post_id=&limit=" + limit + "&offset=" + offset + "&should_bypass_safemode=true";
                string referer = @"https://www.tumblr.com/dashboard/blog/" + blog.Name;
                var headers = new Dictionary<string, string> { { "X-tumblr-form-key", tumblrKey } };
                HttpWebRequest request = webRequestFactory.CreateGetXhrReqeust(url, referer, headers);
                cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
                cookieService.GetUriCookie(request.CookieContainer,
                    new Uri("https://" + blog.Name.Replace("+", "-") + ".tumblr.com"));
                requestRegistration = ct.Register(() => request.Abort());
                return await webRequestFactory.ReadReqestToEnd(request);
            }
            finally
            {
                requestRegistration.Dispose();
            }
        }

        private async Task AddUrlsToDownloadList(TumblrJson response, int crawlerNumber)
        {
            while (true)
            {
                if (CheckifShouldStop())
                    return;

                CheckIfShouldPause();

                if (!CheckPostAge(response))
                {
                    return;
                }

                try
                {
                    foreach (Post post in response.response.posts)
                    {
                        if (!PostWithinTimeSpan(post))
                            continue;
                        if (!CheckIfContainsTaggedPost(post))
                            continue;
                        if (!CheckIfDownloadRebloggedPosts(post))
                            continue;

                        AddPhotoUrlToDownloadList(post);
                        AddVideoUrlToDownloadList(post);
                        AddAudioUrlToDownloadList(post);
                        AddTextUrlToDownloadList(post);
                        AddQuoteUrlToDownloadList(post);
                        AddLinkUrlToDownloadList(post);
                        AddConversationUrlToDownloadList(post);
                        AddAnswerUrlToDownloadList(post);
                        AddPhotoMetaUrlToDownloadList(post);
                        AddVideoMetaUrlToDownloadList(post);
                        AddAudioMetaUrlToDownloadList(post);
                        await AddExternalPhotoUrlToDownloadList(post);
                    }
                }
                catch (NullReferenceException)
                {
                }

                Interlocked.Increment(ref numberOfPagesCrawled);
                UpdateProgressQueueInformation(Resources.ProgressGetUrlShort, numberOfPagesCrawled);

                string document = await GetSvcPageAsync(blog.PageSize.ToString(), (blog.PageSize * crawlerNumber).ToString());
                response = ConvertJsonToClass<TumblrJson>(document);
                if (!response.response.posts.Any() || !string.IsNullOrEmpty(blog.DownloadPages))
                {
                    return;
                }

                crawlerNumber += shellService.Settings.ConcurrentScans;
            }
        }

        private bool CheckPostAge(TumblrJson document)
        {
            ulong highestPostId = 0;
            ulong.TryParse(document.response.posts.FirstOrDefault().id,
                out highestPostId);

            return highestPostId >= GetLastPostId();
        }

        private bool CheckIfDownloadRebloggedPosts(Post post)
        {
            return blog.DownloadRebloggedPosts || post.reblogged_from_tumblr_url == null;
        }

        private void AddToJsonQueue(TumblrCrawlerData<Post> addToList)
        {
            if (blog.DumpCrawlerData)
                jsonQueue.Add(addToList);
        }

        private bool CheckIfContainsTaggedPost(Post post)
        {
            return !tags.Any() || post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase));
        }

        private void AddPhotoUrlToDownloadList(Post post)
        {
            if (!blog.DownloadPhoto)
                return;

            Post postCopy = post;
            if (post.type == "photo")
            {
                AddPhotoUrl(post);
                postCopy = (Post)post.Clone();
                postCopy.photos.Clear();
            }

            AddInlinePhotoUrl(postCopy);
        }

        private void AddPhotoUrl(Post post)
        {
            string postId = post.id;
            foreach (Photo photo in post.photos)
            {
                string imageUrl = photo.alt_sizes.Where(url => url.width == int.Parse(ImageSize())).Select(url => url.url)
                                       .FirstOrDefault() ??
                                  photo.alt_sizes.FirstOrDefault().url;

                if (CheckIfSkipGif(imageUrl))
                    continue;

                AddToDownloadList(new PhotoPost(imageUrl, postId, post.timestamp.ToString()));
                AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
            }
        }

        private void AddInlinePhotoUrl(Post post)
        {
            AddTumblrPhotoUrl(InlineSearch(post));
        }

        private void AddVideoUrlToDownloadList(Post post)
        {
            if (!blog.DownloadVideo)
                return;

            Post postCopy = post;
            if (post.type == "video")
            {
                AddVideoUrl(post);

                postCopy = (Post)post.Clone();
                postCopy.video_url = string.Empty;
            }

            //var videoUrls = new HashSet<string>();

            AddInlineVideoUrl(postCopy);
            //AddInlineTumblrVideoUrl(videoUrls, post, new Regex("\"(https?://ve.media.tumblr.com/(tumblr_[\\w]*))"));
            //AddInlineTumblrVideoUrl(videoUrls, post, new Regex("\"(https?://vtt.tumblr.com/(tumblr_[\\w]*))"));
            AddGenericInlineVideoUrl(postCopy);

            //AddInlineVideoUrlsToDownloader(videoUrls, postCopy);
        }

        private void AddVideoUrl(Post post)
        {
            if (post.video_url == null)
                return;

            string postId = post.id;
            string videoUrl = post.video_url;

            if (shellService.Settings.VideoSize == 480)
            {
                if (!videoUrl.Contains("_480"))
                {
                    videoUrl = videoUrl.Replace(".mp4", "_480.mp4");
                }
            }

            AddToDownloadList(new VideoPost(videoUrl, postId, post.timestamp.ToString()));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
        }

        private void AddInlineVideoUrl(Post post)
        {
            AddTumblrVideoUrl(InlineSearch(post));
        }

        private void AddInlineTumblrVideoUrl(HashSet<string> videoUrls, Post post, Regex regex)
        {
            foreach (Match match in regex.Matches(InlineSearch(post)))
            {
                string videoUrl = match.Groups[1].Value;

                if (shellService.Settings.VideoSize == 480)
                    videoUrl += "_480";

                videoUrls.Add(videoUrl + ".mp4");
            }
        }

        private void AddGenericInlineVideoUrl(Post post)
        {
            AddGenericVideoUrl(InlineSearch(post));
        }

        private void AddInlineVideoUrlsToDownloader(HashSet<string> videoUrls, Post post)
        {
            foreach (string videoUrl in videoUrls)
            {
                AddToDownloadList(new VideoPost(videoUrl, post.id, post.timestamp.ToString()));
            }
        }

        private void AddAudioUrlToDownloadList(Post post)
        {
            if (!blog.DownloadAudio)
                return;
            if (post.type != "audio")
                return;

            string postId = post.id;
            string audioUrl = post.audio_url;
            if (!audioUrl.EndsWith(".mp3"))
                audioUrl = audioUrl + ".mp3";

            AddToDownloadList(new AudioPost(audioUrl, postId, post.timestamp.ToString()));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(audioUrl.Split('/').Last(), ".json"),
                post));
        }

        private void AddTextUrlToDownloadList(Post post)
        {
            if (!blog.DownloadText)
                return;
            if (post.type != "text")
                return;

            string postId = post.id;
            string textBody = tumblrJsonParser.ParseText(post);
            AddToDownloadList(new TextPost(textBody, postId, post.timestamp.ToString()));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private void AddQuoteUrlToDownloadList(Post post)
        {
            if (!blog.DownloadQuote)
                return;
            if (post.type != "quote")
                return;

            string postId = post.id;
            string textBody = tumblrJsonParser.ParseQuote(post);
            AddToDownloadList(new QuotePost(textBody, postId, post.timestamp.ToString()));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private void AddLinkUrlToDownloadList(Post post)
        {
            if (!blog.DownloadLink)
                return;
            if (post.type != "link")
                return;

            string postId = post.id;
            string textBody = tumblrJsonParser.ParseLink(post);
            AddToDownloadList(new LinkPost(textBody, postId, post.timestamp.ToString()));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private void AddConversationUrlToDownloadList(Post post)
        {
            if (!blog.DownloadConversation)
                return;
            if (post.type != "chat")
                return;

            string postId = post.id;
            string textBody = tumblrJsonParser.ParseConversation(post);
            AddToDownloadList(new ConversationPost(textBody, postId, post.timestamp.ToString()));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private void AddAnswerUrlToDownloadList(Post post)
        {
            if (!blog.DownloadAnswer)
                return;
            if (post.type != "answer")
                return;

            string postId = post.id;
            string textBody = tumblrJsonParser.ParseAnswer(post);
            AddToDownloadList(new AnswerPost(textBody, postId, post.timestamp.ToString()));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private void AddPhotoMetaUrlToDownloadList(Post post)
        {
            if (!blog.CreatePhotoMeta)
                return;
            if (post.type != "photo")
                return;

            string postId = post.id;
            string textBody = tumblrJsonParser.ParsePhotoMeta(post);
            AddToDownloadList(new PhotoMetaPost(textBody, postId));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private void AddVideoMetaUrlToDownloadList(Post post)
        {
            if (!blog.CreateVideoMeta)
                return;
            if (post.type != "video")
                return;

            string postId = post.id;
            string textBody = tumblrJsonParser.ParseVideoMeta(post);
            AddToDownloadList(new VideoMetaPost(textBody, postId));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private void AddAudioMetaUrlToDownloadList(Post post)
        {
            if (!blog.CreateAudioMeta)
                return;
            if (post.type != "audio")
                return;

            string postId = post.id;
            string textBody = tumblrJsonParser.ParseAudioMeta(post);
            AddToDownloadList(new AudioMetaPost(textBody, postId));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private string InlineSearch(Post post)
        {
            return string.Join(" ", post.trail.Select(trail => trail.content_raw));
        }

        private async Task AddExternalPhotoUrlToDownloadList(Post post)
        {
            string searchableText = InlineSearch(post);
            string timestamp = post.timestamp.ToString();

            if (blog.DownloadImgur) AddImgurUrl(searchableText, timestamp);

            if (blog.DownloadImgur) await AddImgurAlbumUrl(searchableText, timestamp);

            if (blog.DownloadGfycat) await AddGfycatUrl(searchableText, timestamp);

            if (blog.DownloadWebmshare) AddWebmshareUrl(searchableText, timestamp);

            if (blog.DownloadMixtape) AddMixtapeUrl(searchableText, timestamp);

            if (blog.DownloadUguu) AddUguuUrl(searchableText, timestamp);

            if (blog.DownloadSafeMoe) AddSafeMoeUrl(searchableText, timestamp);

            if (blog.DownloadLoliSafe) AddLoliSafeUrl(searchableText, timestamp);

            if (blog.DownloadCatBox) AddCatBoxUrl(searchableText, timestamp);
        }
    }
}
