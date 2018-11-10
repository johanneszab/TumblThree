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
    public class TumblrHiddenCrawler : AbstractTumblrCrawler, ICrawler
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

        private string tumblrKey = string.Empty;

        private bool completeGrab = true;
        private bool incompleteCrawl = false;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        public TumblrHiddenCrawler(IShellService shellService, CancellationToken ct, PauseToken pt,
            IProgress<DownloadProgress> progress, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IDownloader downloader, ICrawlerDataDownloader crawlerDataDownloader,
            ITumblrToTextParser<Post> tumblrJsonParser, IImgurParser imgurParser, IGfycatParser gfycatParser,
            IWebmshareParser webmshareParser, IMixtapeParser mixtapeParser, IUguuParser uguuParser, ISafeMoeParser safemoeParser,
            ILoliSafeParser lolisafeParser, ICatBoxParser catboxParser, IPostQueue<TumblrPost> postQueue,
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
            catch (WebException webException) when ((webException.Response != null))
            {
                if (WebExceptionServiceUnavailable(webException))
                    blog.Online = true;

                if (WebExceptionNotFound(webException))
                    blog.Online = false;

                if (WebExceptionLimitExceeded(webException))
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
            catch (WebException webException) when ((webException.Response != null))
            {
                WebExceptionServiceUnavailable(webException);
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
                trackedTasks.Add(new Func<Task>(async () => { await CrawlPage(pageNumber); })());
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
                if (WebExceptionLimitExceeded(webException))
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
                WebExceptionLimitExceeded(webException);
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
                if (WebExceptionServiceUnavailable(webException))
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
            if (!shellService.Settings.LimitConnections)
                return await RequestDataAsync(limit, offset);

            crawlerService.Timeconstraint.Acquire();
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
            if (!blog.DownloadRebloggedPosts)
            {
                return post.reblogged_from_tumblr_url == null;
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
            var regex = new Regex("\"(http[A-Za-z0-9_/:.]*media.tumblr.com[A-Za-z0-9_/:.]*(jpg|png|gif))\"");
            foreach (Match match in regex.Matches(InlineSearch(post)))
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

            var videoUrls = new HashSet<string>();

            AddInlineVideoUrl(videoUrls, postCopy);
            //AddInlineVttTumblrVideoUrl(videoUrls, postCopy);
            //AddInlineVeTumblrVideoUrl(videoUrls, postCopy);
            AddGenericInlineVideoUrl(videoUrls, postCopy);

            AddInlineVideoUrlsToDownloader(videoUrls, postCopy);
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

        private void AddInlineVideoUrl(HashSet<string> videoUrls, Post post)
        {
            var regex = new Regex("src=\"(http[A-Za-z0-9_/:.]*video_file[\\S]*/(tumblr_[\\w]*))[0-9/]*\"");
            foreach (Match match in regex.Matches(InlineSearch(post)))
            {
                string videoUrl = match.Groups[2].Value;

                if (shellService.Settings.VideoSize == 480)
                {
                    videoUrl += "_480";
                }

                videoUrls.Add("https://vtt.tumblr.com/" + videoUrl + ".mp4");
            }
        }

        private void AddInlineVttTumblrVideoUrl(HashSet<string> videoUrls, Post post)
        {
            var regex = new Regex("\"(https?://vtt.tumblr.com/(tumblr_[\\w]*))");
            foreach (Match match in regex.Matches(InlineSearch(post)))
            {
                string videoUrl = match.Groups[1].Value;

                if (shellService.Settings.VideoSize == 480)
                {
                    videoUrl += "_480";
                }

                videoUrls.Add(videoUrl + ".mp4");
            }
        }

        private void AddInlineVeTumblrVideoUrl(HashSet<string> videoUrls, Post post)
        {
            var regex = new Regex("\"(https?://ve.media.tumblr.com/(tumblr_[\\w]*))");
            foreach (Match match in regex.Matches(InlineSearch(post)))
            {
                string videoUrl = match.Groups[1].Value;

                if (shellService.Settings.VideoSize == 480)
                {
                    videoUrl += "_480";
                }

                videoUrls.Add(videoUrl + ".mp4");
            }
        }

        private void AddGenericInlineVideoUrl(HashSet<string> videoUrls, Post post)
        {
            var regex = new Regex("\"(https?://(?:[a-z0-9\\-]+\\.)+[a-z]{2,6}(?:/[^/#?]+)+\\.(?:mp4|mkv))\"");

            foreach (Match match in regex.Matches(InlineSearch(post)))
            {
                string videoUrl = match.Groups[1].Value;

                if (videoUrl.Contains("tumblr") && shellService.Settings.VideoSize == 480)
                {
                    int indexOfSuffix = videoUrl.LastIndexOf('.');
                    if (indexOfSuffix >= 0)
                        videoUrl = videoUrl.Insert(indexOfSuffix, "_480");
                }

                videoUrls.Add(videoUrl);
            }
        }

        private void AddInlineVideoUrlsToDownloader(HashSet<string> videoUrls, Post post)
        {
            foreach (string videoUrl in videoUrls)
            {
                AddToDownloadList(new VideoPost(videoUrl, post.id, post.timestamp.ToString()));
                //AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
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
            if (blog.DownloadImgur) await AddImgurUrl(post);

            if (blog.DownloadGfycat) await AddGfycatUrl(post);

            if (blog.DownloadWebmshare) AddWebmshareUrl(post);

            if (blog.DownloadMixtape) AddMixtapeUrl(post);

            if (blog.DownloadUguu) AddUguuUrl(post);

            if (blog.DownloadSafeMoe) AddSafeMoeUrl(post);

            if (blog.DownloadLoliSafe) AddLoliSafeUrl(post);

            if (blog.DownloadCatBox) AddCatBoxUrl(post);
        }

        private async Task AddImgurUrl(Post post)
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
                AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"),
                    post));
            }

            // album urls
            regex = imgurParser.GetImgurAlbumRegex();
            foreach (Match match in regex.Matches(post.caption))
            {
                string albumUrl = match.Groups[1].Value;
                string imgurId = match.Groups[2].Value;
                string album = await imgurParser.RequestImgurAlbumSite(albumUrl);

                Regex hashRegex = imgurParser.GetImgurAlbumHashRegex();
                MatchCollection hashMatches = hashRegex.Matches(album);
                List<string> hashes = hashMatches.Cast<Match>().Select(hashMatch => hashMatch.Groups[1].Value).ToList();

                Regex extRegex = imgurParser.GetImgurAlbumExtRegex();
                MatchCollection extMatches = extRegex.Matches(album);
                List<string> exts = extMatches.Cast<Match>().Select(extMatch => extMatch.Groups[1].Value).ToList();

                IEnumerable<string> imageUrls = hashes.Zip(exts, (hash, ext) => "https://i.imgur.com/" + hash + ext);

                foreach (string imageUrl in imageUrls)
                {
                    if (blog.SkipGif && (imageUrl.EndsWith(".gif") || imageUrl.EndsWith(".gifv")))
                        continue;
                    AddToDownloadList(new ExternalPhotoPost(imageUrl, imgurId,
                        post.timestamp.ToString()));
                    AddToJsonQueue(
                        new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
                }
            }
        }

        private async Task AddGfycatUrl(Post post)
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
                AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"),
                    post));
            }
        }

        private void AddWebmshareUrl(Post post)
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
                AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"),
                    post));
            }
        }

        private void AddMixtapeUrl(Post post)
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
                    AddToJsonQueue(
                        new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
                }
            }
        }

        private void AddUguuUrl(Post post)
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
                    AddToJsonQueue(
                        new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
                }
            }
        }

        private void AddSafeMoeUrl(Post post)
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
                    AddToJsonQueue(
                        new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
                }
            }
        }

        private void AddLoliSafeUrl(Post post)
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
                    AddToJsonQueue(
                        new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
                }
            }
        }

        private void AddCatBoxUrl(Post post)
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
                    AddToJsonQueue(
                        new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
                }
            }
        }
    }
}
