using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Applications.Parser;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.DataModels.TumblrCrawlerData;
using TumblThree.Applications.DataModels.TumblrSvcJson;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(TumblrHiddenBlog))]
    public class TumblrHiddenCrawler : TumblrAbstractCrawler, ICrawler
    {
        private readonly IDownloader downloader;
        private readonly PauseToken pt;
        private readonly ITumblrToTextParser<Post> tumblrJsonParser;
        private readonly IImgurParser imgurParser;
        private readonly IGfycatParser gfycatParser;
        private readonly IWebmshareParser webmshareParser;
        private readonly IMixtapeParser mixtapeParser;
        private readonly IUguuParser uguuParser;
        private readonly ISafeMoeParser safemoeParser;
        private readonly ILoliSafeParser lolisafeParser;
        private readonly ICatBoxParser catboxParser;
        private readonly IPostQueue<TumblrCrawlerData<Post>> jsonQueue;
        private readonly ICrawlerDataDownloader crawlerDataDownloader;

        private string passwordAuthentication = string.Empty;
        private string tumblrKey = string.Empty;

        public TumblrHiddenCrawler(IShellService shellService, CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress,
            ICrawlerService crawlerService, IWebRequestFactory webRequestFactory, ISharedCookieService cookieService, IDownloader downloader,
            ICrawlerDataDownloader crawlerDataDownloader, ITumblrToTextParser<Post> tumblrJsonParser, IImgurParser imgurParser,
            IGfycatParser gfycatParser, IWebmshareParser webmshareParser, IMixtapeParser mixtapeParser, IUguuParser uguuParser,
            ISafeMoeParser safemoeParser, ILoliSafeParser lolisafeParser, ICatBoxParser catboxParser, IPostQueue<TumblrPost> postQueue,
            IPostQueue<TumblrCrawlerData<Post>> jsonQueue, IBlog blog)
            : base(shellService, crawlerService, ct, progress, webRequestFactory, cookieService, postQueue, blog)
        {
            this.downloader = downloader;
            this.pt = pt;
            this.tumblrJsonParser = tumblrJsonParser;
            this.imgurParser = imgurParser;
            this.gfycatParser = gfycatParser;
            this.webmshareParser = webmshareParser;
            this.mixtapeParser = mixtapeParser;
            this.uguuParser = uguuParser;
            this.safemoeParser = safemoeParser;
            this.lolisafeParser = lolisafeParser;
            this.catboxParser = catboxParser;
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
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.ProtocolError && webException.Response != null)
                {
                    var resp = (HttpWebResponse)webException.Response;
                    if (resp.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        Logger.Error("TumblrHiddenCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                        shellService.ShowError(webException, Resources.NotLoggedIn, blog.Name);
                        blog.Online = true;
                        return;
                    }
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        Logger.Error("TumblrBlogCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                        shellService.ShowError(webException, Resources.BlogIsOffline, blog.Name);
                        blog.Online = false;
                        return;
                    }
                    // 429: Too Many Requests
                    if ((int)resp.StatusCode == 429)
                    {
                        Logger.Error("TumblrHiddenCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                        shellService.ShowError(webException, Resources.LimitExceeded, blog.Name);
                        blog.Online = true;
                        return;
                    }
                }
            }
            catch (TimeoutException timeoutException)
            {
                Logger.Error("TumblrHiddenCrawler:IsBlogOnlineAsync:WebException {0}", timeoutException);
                shellService.ShowError(timeoutException, Resources.TimeoutReached, Resources.OnlineChecking, blog.Name);
                blog.Online = false;
            }
        }

        public override async Task UpdateMetaInformationAsync()
        {
            try
            {
                if (!blog.Online)
                {
                    return;
                }
                tumblrKey = await UpdateTumblrKey("https://www.tumblr.com/dashboard/blog/" + blog.Name);
                string document = await GetSvcPageAsync("1", "0");
                var response = ConvertJsonToClass<TumblrJson>(document);

                if (response.meta.status == 200)
                {
                    blog.Title = response.response.posts.FirstOrDefault().blog.title;
                    blog.Description = response.response.posts.FirstOrDefault().blog.description;
                }
            }
            catch (WebException webException)
            {
                var resp = (HttpWebResponse)webException.Response;
                if (resp.StatusCode == HttpStatusCode.ServiceUnavailable)
                { 
                    Logger.Error("TumblrHiddenCrawler:GetUrlsAsync: {0}", "User not logged in");
                    shellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, blog.Name);
                }
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

            if (string.IsNullOrEmpty(blog.DownloadPages))
            {
                return Enumerable.Range(0, shellService.Settings.ConcurrentScans);
            }
            return RangeToSequence(blog.DownloadPages);
        }

        private async Task<Tuple<ulong, bool>> GetUrlsAsync()
        {
            var semaphoreSlim = new SemaphoreSlim(shellService.Settings.ConcurrentScans);
            var trackedTasks = new List<Task>();
            var incompleteCrawl = false;
            ulong highestId = 0;

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

                trackedTasks.Add(new Func<Task>(async () =>
                {
                    if (!string.IsNullOrWhiteSpace(blog.Tags))
                    {
                        tags = blog.Tags.Split(',').Select(x => x.Trim()).ToList();
                    }

                    try
                    {
                        string document = await GetSvcPageAsync(blog.PageSize.ToString(), (blog.PageSize * pageNumber).ToString());
                        var response = ConvertJsonToClass<TumblrJson>(document);
                        await AddUrlsToDownloadList(response, pageNumber);
                    }
                    catch (WebException webException) when ((webException.Response != null))
                    {
                        var resp = (HttpWebResponse)webException.Response;
                        // 429: Too Many Requests
                        if ((int)resp.StatusCode == 429)
                        {
                            // TODO: add retry logic?
                            incompleteCrawl = true;
                            Logger.Error("TumblrHiddenCrawler:GetUrlsAsync:WebException {0}", webException);
                            shellService.ShowError(webException, Resources.LimitExceeded, blog.Name);
                        }
                    }
                    catch (TimeoutException timeoutException)
                    {
                        incompleteCrawl = true;
                        Logger.Error("TumblrHiddenCrawler:GetUrlsAsync:WebException {0}", timeoutException);
                        shellService.ShowError(timeoutException, Resources.TimeoutReached, Resources.Crawling, blog.Name);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }
                })());
            }
            await Task.WhenAll(trackedTasks);

            jsonQueue.CompleteAdding();
            postQueue.CompleteAdding();

            UpdateBlogStats();

            return new Tuple<ulong, bool>(highestId, incompleteCrawl);
        }

        private async Task<ulong> GetHighestPostIdAsync()
        {
            try
            {
                return await GetHighestPostId();
            }
            catch (WebException webException)
            {
                // 429: Too Many Requests
                var webRespStatusCode = (int)((HttpWebResponse)webException?.Response).StatusCode;
                if (webRespStatusCode == 429)
                {
                    Logger.Error("TumblrHiddenCrawler:GetHighestPostIdAsync:WebException {0}", webException);
                    shellService.ShowError(webException, Resources.LimitExceeded, blog.Name);
                }
                return 0;
            }
            catch (TimeoutException timeoutException)
            {
                Logger.Error("TumblrHiddenCrawler:GetHighestPostIdAsync:WebException {0}", timeoutException);
                shellService.ShowError(timeoutException, Resources.TimeoutReached, Resources.Crawling, blog.Name);
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
            if (!(string.IsNullOrEmpty(blog.DownloadFrom) && string.IsNullOrEmpty(blog.DownloadTo)))
            {
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
                if (downloadFromUnixTime >= postTime || postTime >= downloadToUnixTime)
                    return false;
            }
            return true;
        }

        private async Task<bool> CheckIfLoggedInAsync()
        {
            try
            {
                string document = await GetSvcPageAsync(blog.PageSize.ToString(), (blog.PageSize * 1).ToString());
            }
            catch (WebException webException)
            {
                var webRespStatusCode = (int)((HttpWebResponse)webException?.Response).StatusCode;
                if (webRespStatusCode == 503)
                {
                    return false;
                }
            }
            catch (TimeoutException timeoutException)
            {
                Logger.Error("TumblrHiddenCrawler:CheckIfLoggedIn:WebException {0}", timeoutException);
                shellService.ShowError(timeoutException, Resources.TimeoutReached, Resources.Crawling, blog.Name);
                return false;
            }
            return true;
        }

        private async Task<string> GetSvcPageAsync(string limit, string offset)
        {
            if (shellService.Settings.LimitConnections)
            {
                crawlerService.Timeconstraint.Acquire();
                return await RequestDataAsync(limit, offset);
            }
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
                cookieService.GetUriCookie(request.CookieContainer, new Uri("https://" + blog.Name.Replace("+", "-") + ".tumblr.com"));
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
                if (ct.IsCancellationRequested)
                {
                    return;
                }
                if (pt.IsPaused)
                {
                    pt.WaitWhilePausedWithResponseAsyc().Wait();
                }

                if (!CheckPostAge(response))
                {
                    return;
                }

                try
                {
                    AddPhotoUrlToDownloadList(response);
                    AddVideoUrlToDownloadList(response);
                    AddAudioUrlToDownloadList(response);
                    AddTextUrlToDownloadList(response);
                    AddQuoteUrlToDownloadList(response);
                    AddLinkUrlToDownloadList(response);
                    AddConversationUrlToDownloadList(response);
                    AddAnswerUrlToDownloadList(response);
                    AddPhotoMetaUrlToDownloadList(response);
                    AddVideoMetaUrlToDownloadList(response);
                    AddAudioMetaUrlToDownloadList(response);
                    await AddExternalPhotoUrlToDownloadList(response);
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

            if (highestPostId < GetLastPostId())
            {
                return false;
            }
            return true;
        }

        private bool CheckIfDownloadRebloggedPosts(Post post)
        {
            if (!blog.DownloadRebloggedPosts)
            {
                if (post.reblogged_from_tumblr_url == null)
                    return true;
                return false;
            }
            return true;
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

        private void AddPhotoUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadPhoto)
            {
                foreach (Post post in document.response.posts)
                {
                    if (!PostWithinTimeSpan(post))
                        continue;
                    if (post.type == "photo" && CheckIfContainsTaggedPost(post))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            AddPhotoUrl(post);
                            if (post.caption != null)
                            {
                                var postCopy = (Post)post.Clone();
                                postCopy.photos.Clear();
                                AddInlinePhotoUrl(postCopy);
                            }
                        }
                    }
                    // check for inline images
                    if (post.type != "photo" && CheckIfContainsTaggedPost(post))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                            AddInlinePhotoUrl(post);
                    }
                }
            }
        }

        private void AddPhotoUrl(Post post)
        {
            string postId = post.id;
            foreach (Photo photo in post.photos)
            {
                string imageUrl = photo.alt_sizes.Where(url => url.width == int.Parse(ImageSize())).Select(url => url.url).FirstOrDefault();
                if (imageUrl == null)
                    imageUrl = photo.alt_sizes.FirstOrDefault().url;

                if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                {
                    continue;
                }

                AddToDownloadList(new PhotoPost(imageUrl, postId, post.timestamp.ToString()));
                AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
            }
        }

        private void AddInlinePhotoUrl(Post post)
        {
            if (post.caption == null)
                return;
            var regex = new Regex("\"(http[A-Za-z0-9_/:.]*media.tumblr.com[A-Za-z0-9_/:.]*(jpg|png|gif))\"");
            foreach (Match match in regex.Matches(post.caption))
            {
                string postId = post.id;

                string imageUrl = match.Groups[1].Value;
                if (imageUrl.Contains("avatar") || imageUrl.Contains("previews"))
                    continue;
                if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                {
                    continue;
                }
                AddToDownloadList(new PhotoPost(imageUrl, postId, post.timestamp.ToString()));
                //AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
            }
        }


        private void AddVideoUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadVideo)
            {
                foreach (Post post in document.response.posts)
                {
                    if (!PostWithinTimeSpan(post))
                        continue;
                    if (post.type == "video" && CheckIfContainsTaggedPost(post))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            AddVideoUrl(post);
                            if (post.caption != null)
                            {
                                var postCopy = (Post)post.Clone();
                                postCopy.video_url = string.Empty;
                                AddInlineVideoUrl(postCopy);
                            }
                        }
                    }
                    // check for inline videos
                    if (post.type != "video" && CheckIfContainsTaggedPost(post))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                            AddInlineVideoUrl(post);
                    }
                }
            }
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
                    videoUrl.Replace(".mp4", "_480.mp4");
                }
            }
            AddToDownloadList(new VideoPost(videoUrl, postId, post.timestamp.ToString()));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
        }

        private void AddInlineVideoUrl(Post post)
        {
            if (post.caption == null)
                return;
            var regex = new Regex("\"(http[A-Za-z0-9_/:.]*.com/video_file/[A-Za-z0-9_/:.]*)\"");
            foreach (Match match in regex.Matches(post.caption))
            {
                string videoUrl = match.Groups[1].Value;
                if (shellService.Settings.VideoSize == 1080)
                {
                    AddToDownloadList(new VideoPost(videoUrl.Replace("/480", "") + ".mp4", post.id, post.timestamp.ToString()));
                    //AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
                }
                else if (shellService.Settings.VideoSize == 480)
                {
                    AddToDownloadList(new VideoPost(
                        "https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + "_480.mp4",
                        post.id, post.timestamp.ToString()));
                    //AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
                }
            }
        }

        private void AddAudioUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadAudio)
            {
                foreach (Post post in document.response.posts)
                {
                    if (!PostWithinTimeSpan(post))
                        continue;
                    if (post.type == "audio" && CheckIfContainsTaggedPost(post))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string audioUrl = post.audio_url;
                            if (!audioUrl.EndsWith(".mp3"))
                                audioUrl = audioUrl + ".mp3";
                            AddToDownloadList(new AudioPost(audioUrl, postId, post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(audioUrl.Split('/').Last(), ".json"), post));
                        }
                    }
                }
            }
        }

        private void AddTextUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadText)
            {
                foreach (Post post in document.response.posts)
                {
                    if (!PostWithinTimeSpan(post))
                        continue;
                    if (post.type == "text" && (CheckIfContainsTaggedPost(post)))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = tumblrJsonParser.ParseText(post);
                            AddToDownloadList(new TextPost(textBody, postId, post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
                        }
                    }
                }
            }
        }

        private void AddQuoteUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadQuote)
            {
                foreach (Post post in document.response.posts)
                {
                    if (!PostWithinTimeSpan(post))
                        continue;
                    if (post.type == "quote" && CheckIfContainsTaggedPost(post))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = tumblrJsonParser.ParseQuote(post);
                            AddToDownloadList(new QuotePost(textBody, postId, post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
                        }
                    }
                }
            }
        }

        private void AddLinkUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadLink)
            {
                foreach (Post post in document.response.posts)
                {
                    if (!PostWithinTimeSpan(post))
                        continue;
                    if (post.type == "link" && CheckIfContainsTaggedPost(post))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = tumblrJsonParser.ParseLink(post);
                            AddToDownloadList(new LinkPost(textBody, postId, post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
                        }
                    }
                }
            }
        }

        private void AddConversationUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadConversation)
            {
                foreach (Post post in document.response.posts)
                {
                    if (!PostWithinTimeSpan(post))
                        continue;
                    if (post.type == "chat" && CheckIfContainsTaggedPost(post))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = tumblrJsonParser.ParseConversation(post);
                            AddToDownloadList(new ConversationPost(textBody, postId, post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
                        }
                    }
                }
            }
        }

        private void AddAnswerUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadAnswer)
            {
                foreach (Post post in document.response.posts)
                {
                    if (!PostWithinTimeSpan(post))
                        continue;
                    if (post.type == "answer" && CheckIfContainsTaggedPost(post))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = tumblrJsonParser.ParseAnswer(post);
                            AddToDownloadList(new AnswerPost(textBody, postId, post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
                        }
                    }
                }
            }
        }

        private void AddPhotoMetaUrlToDownloadList(TumblrJson document)
        {
            if (blog.CreatePhotoMeta)
            {
                foreach (Post post in document.response.posts)
                {
                    if (!PostWithinTimeSpan(post))
                        continue;
                    if (post.type == "photo" && CheckIfContainsTaggedPost(post))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = tumblrJsonParser.ParsePhotoMeta(post);
                            AddToDownloadList(new PhotoMetaPost(textBody, postId));
                            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
                        }
                    }
                }
            }
        }

        private void AddVideoMetaUrlToDownloadList(TumblrJson document)
        {
            if (blog.CreateVideoMeta)
            {
                foreach (Post post in document.response.posts)
                {
                    if (!PostWithinTimeSpan(post))
                        continue;
                    if (post.type == "video" && CheckIfContainsTaggedPost(post))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = tumblrJsonParser.ParseVideoMeta(post);
                            AddToDownloadList(new VideoMetaPost(textBody, postId));
                            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
                        }
                    }
                }
            }
        }

        private void AddAudioMetaUrlToDownloadList(TumblrJson document)
        {
            if (blog.CreateAudioMeta)
            {
                foreach (Post post in document.response.posts)
                {
                    if (!PostWithinTimeSpan(post))
                        continue;
                    if (post.type == "audio" && CheckIfContainsTaggedPost(post))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = tumblrJsonParser.ParseAudioMeta(post);
                            AddToDownloadList(new AudioMetaPost(textBody, postId));
                            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
                        }
                    }
                }
            }
        }

        private async Task AddExternalPhotoUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadImgur) await AddImgurUrl(document);

            if (blog.DownloadGfycat) await AddGfycatUrl(document);

            if (blog.DownloadWebmshare) AddWebmshareUrl(document);

            if (blog.DownloadMixtape) AddMixtapeUrl(document);

            if (blog.DownloadUguu) AddUguuUrl(document);

            if (blog.DownloadSafeMoe) AddSafeMoeUrl(document);

            if (blog.DownloadLoliSafe) AddLoliSafeUrl(document);

            if (blog.DownloadCatBox) AddCatBoxUrl(document);
        }

        private async Task AddImgurUrl(TumblrJson document)
        {
            foreach (Post post in document.response.posts)
            {
                if (!PostWithinTimeSpan(post))
                    continue;
                if (CheckIfContainsTaggedPost(post))
                {
                    if (CheckIfDownloadRebloggedPosts(post))
                    {
                        // single linked images
                        Regex regex = imgurParser.GetImgurImageRegex();
                        foreach (Match match in regex.Matches(post.caption))
                        {
                            string imageUrl = match.Groups[1].Value;
                            string imgurId = match.Groups[2].Value;
                            if (blog.SkipGif && (imageUrl.EndsWith(".gif") || imageUrl.EndsWith(".gifv")))
                            {
                                continue;
                            }
                            AddToDownloadList(new ExternalPhotoPost(imageUrl, imgurId,
                                post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
                        }

                        // album urls
                        regex = imgurParser.GetImgurAlbumRegex();
                        foreach (Match match in regex.Matches(post.caption))
                        {
                            string albumUrl = match.Groups[1].Value;
                            string imgurId = match.Groups[2].Value;
                            string album = await imgurParser.RequestImgurAlbumSite(albumUrl);

                            Regex hashRegex = imgurParser.GetImgurAlbumHashRegex();
                            var hashMatches = hashRegex.Matches(album);
                            var hashes = hashMatches.Cast<Match>().Select(hashMatch => hashMatch.Groups[1].Value).ToList();

                            Regex extRegex = imgurParser.GetImgurAlbumExtRegex();
                            var extMatches = extRegex.Matches(album);
                            var exts = extMatches.Cast<Match>().Select(extMatch => extMatch.Groups[1].Value).ToList();

                            var imageUrls = hashes.Zip(exts, (hash, ext) => "https://i.imgur.com/" + hash + ext);

                            foreach (string imageUrl in imageUrls)
                            {
                                if (blog.SkipGif && (imageUrl.EndsWith(".gif") || imageUrl.EndsWith(".gifv")))
                                    continue;
                                AddToDownloadList(new ExternalPhotoPost(imageUrl, imgurId,
                                    post.timestamp.ToString()));
                                AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
                            }
                        }
                    }
                }
            }
        }

        private async Task AddGfycatUrl(TumblrJson document)
        {
            foreach (Post post in document.response.posts)
            {
                if (!PostWithinTimeSpan(post))
                    continue;
                if (CheckIfContainsTaggedPost(post))
                {
                    if (CheckIfDownloadRebloggedPosts(post))
                    {
                        Regex regex = gfycatParser.GetGfycatUrlRegex();
                        foreach (Match match in regex.Matches(post.caption))
                        {
                            string gfyId = match.Groups[2].Value;
                            string videoUrl = gfycatParser.ParseGfycatCajaxResponse(await gfycatParser.RequestGfycatCajax(gfyId),
                                blog.GfycatType);
                            if (blog.SkipGif && (videoUrl.EndsWith(".gif") || videoUrl.EndsWith(".gifv")))
                            {
                                continue;
                            }
                            // TODO: postID
                            AddToDownloadList(new VideoPost(videoUrl, gfyId,
                                post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
                        }
                    }
                }
            }
        }

        private void AddWebmshareUrl(TumblrJson document)
        {
            foreach (Post post in document.response.posts)
            {
                if (!PostWithinTimeSpan(post))
                {
                    continue;
                }
                if (CheckIfContainsTaggedPost(post))
                {
                    if (CheckIfDownloadRebloggedPosts(post))
                    {
                        Regex regex = webmshareParser.GetWebmshareUrlRegex();
                        foreach (Match match in regex.Matches(post.caption))
                        {
                            string url = match.Groups[0].Value.Split('\"').First();
                            string webmshareId = match.Groups[2].Value;
                            string imageUrl = webmshareParser.CreateWebmshareUrl(webmshareId, url, blog.WebmshareType);
                            if (blog.SkipGif && (imageUrl.EndsWith(".gif") || imageUrl.EndsWith(".gifv")))
                            {
                                continue;
                            }
                            // TODO: postID
                            AddToDownloadList(new VideoPost(imageUrl, webmshareId,
                                post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
                        }
                    }
                }
            }
        }

        private void AddMixtapeUrl(TumblrJson document)
        {
            foreach (Post post in document.response.posts)
            {
                if (!PostWithinTimeSpan(post))
                {
                    continue;
                }

                if (CheckIfContainsTaggedPost(post))
                {
                    if (CheckIfDownloadRebloggedPosts(post))
                    {
                        Regex regex = mixtapeParser.GetMixtapeUrlRegex();
                        string[] parts = post.caption.Split(new string[] { "href=" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string part in parts)
                        {

                            foreach (Match match in regex.Matches(part))
                            {
                                string temp = match.Groups[0].ToString();
                                string id = match.Groups[2].Value;
                                string url = temp.Split('\"').First();

                                string imageUrl = mixtapeParser.CreateMixtapeUrl(id, url, blog.MixtapeType);
                                if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                                {
                                    continue;
                                }

                                AddToDownloadList(new ExternalVideoPost(imageUrl, id,
                                    post.timestamp.ToString()));
                                AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));

                            }

                        }
                    }
                }
            }
        }

        private void AddUguuUrl(TumblrJson document)
        {
            foreach (Post post in document.response.posts)
            {
                if (!PostWithinTimeSpan(post))
                {
                    continue;
                }

                if (CheckIfContainsTaggedPost(post))
                {
                    if (CheckIfDownloadRebloggedPosts(post))
                    {
                        Regex regex = uguuParser.GetUguuUrlRegex();
                        string[] parts = post.caption.Split(new string[] { "href=" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string part in parts)
                        {

                            foreach (Match match in regex.Matches(part))
                            {
                                string temp = match.Groups[0].ToString();
                                string id = match.Groups[2].Value;
                                string url = temp.Split('\"').First();


                                string imageUrl = uguuParser.CreateUguuUrl(id, url, blog.UguuType);
                                if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                                {
                                    continue;
                                }

                                AddToDownloadList(new ExternalVideoPost(imageUrl, id,
                                    post.timestamp.ToString()));
                                AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));

                            }

                        }
                    }
                }
            }
        }

        private void AddSafeMoeUrl(TumblrJson document)
        {
            foreach (Post post in document.response.posts)
            {
                if (!PostWithinTimeSpan(post))
                {
                    continue;
                }

                if (CheckIfContainsTaggedPost(post))
                {
                    if (CheckIfDownloadRebloggedPosts(post))
                    {
                        Regex regex = safemoeParser.GetSafeMoeUrlRegex();
                        string[] parts = post.caption.Split(new string[] { "href=" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string part in parts)
                        {

                            foreach (Match match in regex.Matches(part))
                            {
                                string temp = match.Groups[0].ToString();
                                string id = match.Groups[2].Value;
                                string url = temp.Split('\"').First();


                                string imageUrl = safemoeParser.CreateSafeMoeUrl(id, url, blog.SafeMoeType);
                                if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                                {
                                    continue;
                                }

                                AddToDownloadList(new ExternalVideoPost(imageUrl, id,
                                    post.timestamp.ToString()));
                                AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));

                            }

                        }
                    }
                }
            }
        }

        private void AddLoliSafeUrl(TumblrJson document)
        {
            foreach (Post post in document.response.posts)
            {
                if (!PostWithinTimeSpan(post))
                {
                    continue;
                }

                if (CheckIfContainsTaggedPost(post))
                {
                    if (CheckIfDownloadRebloggedPosts(post))
                    {
                        Regex regex = lolisafeParser.GetLoliSafeUrlRegex();
                        string[] parts = post.caption.Split(new string[] { "href=" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string part in parts)
                        {

                            foreach (Match match in regex.Matches(part))
                            {
                                string temp = match.Groups[0].ToString();
                                string id = match.Groups[2].Value;
                                string url = temp.Split('\"').First();

                                string imageUrl = lolisafeParser.CreateLoliSafeUrl(id, url, blog.LoliSafeType);
                                if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                                {
                                    continue;
                                }

                                AddToDownloadList(new ExternalVideoPost(imageUrl, id,
                                    post.timestamp.ToString()));
                                AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));

                            }

                        }
                    }
                }
            }
        }

        private void AddCatBoxUrl(TumblrJson document)
        {
            foreach (Post post in document.response.posts)
            {
                if (!PostWithinTimeSpan(post))
                {
                    continue;
                }

                if (CheckIfContainsTaggedPost(post))
                {
                    if (CheckIfDownloadRebloggedPosts(post))
                    {
                        Regex regex = catboxParser.GetCatBoxUrlRegex();
                        string[] parts = post.caption.Split(new string[] { "href=" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string part in parts)
                        {

                            foreach (Match match in regex.Matches(part))
                            {
                                string temp = match.Groups[0].ToString();
                                string id = match.Groups[2].Value;
                                string url = temp.Split('\"').First();

                                string imageUrl = catboxParser.CreateCatBoxUrl(id, url, blog.CatBoxType);
                                if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                                {
                                    continue;
                                }

                                AddToDownloadList(new ExternalVideoPost(imageUrl, id,
                                    post.timestamp.ToString()));
                                AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));

                            }

                        }
                    }
                }
            }
        }
    }
}
