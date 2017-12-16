using System;
using System.Collections.Concurrent;
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

using TumblThree.Applications.DataModels;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Applications.DataModels.TumblrSvcJson;
using TumblThree.Applications.Extensions;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.DataModels.TumblrCrawlerData;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(TumblrHiddenBlog))]
    public class TumblrBlogCrawler : AbstractCrawler, ICrawler
    {
        private readonly ICrawlerService crawlerService;
        private readonly IDownloader downloader;
        private readonly PauseToken pt;
        private readonly IImgurParser imgurParser;
        private readonly IGfycatParser gfycatParser;
        private readonly IWebmshareParser webmshareParser;
        private readonly BlockingCollection<TumblrCrawlerJsonData> jsonQueue;
        private readonly ICrawlerDataDownloader crawlerDataDownloader;

        private string authentication = string.Empty;

        public TumblrBlogCrawler(IShellService shellService, CancellationToken ct, PauseToken pt,
            IProgress<DownloadProgress> progress, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory, ISharedCookieService cookieService, IDownloader downloader, ICrawlerDataDownloader crawlerDataDownloader, IImgurParser imgurParser, IGfycatParser gfycatParser, IWebmshareParser webmshareParser, BlockingCollection<TumblrPost> producerConsumerCollection, BlockingCollection<TumblrCrawlerJsonData> jsonQueue, IBlog blog)
            : base(shellService, ct, progress, webRequestFactory, cookieService, producerConsumerCollection, blog)
        {
            this.crawlerService = crawlerService;
            this.downloader = downloader;
            this.pt = pt;
            this.imgurParser = imgurParser;
            this.gfycatParser = gfycatParser;
            this.webmshareParser = webmshareParser;
            this.jsonQueue = jsonQueue;
            this.crawlerDataDownloader = crawlerDataDownloader;
        }

        public override async Task IsBlogOnlineAsync()
        {
            try
            {
                // Hidden and password protected blogs don't exist?
                await UpdateAuthentication();
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
                        Logger.Error("TumblrBlogCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                        shellService.ShowError(webException, Resources.NotLoggedIn, blog.Name);
                        blog.Online = true;
                        return;
                    }
                }
                if (webException.Status == WebExceptionStatus.ProtocolError && webException.Response != null)
                {
                    var resp = (HttpWebResponse)webException.Response;
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        Logger.Error("TumblrBlogCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                        shellService.ShowError(webException, Resources.BlogIsOffline, blog.Name);
                        blog.Online = false;
                        return;
                    }
                }
                if (webException.Status == WebExceptionStatus.ProtocolError && webException.Response != null)
                {
                    var resp = (HttpWebResponse)webException.Response;
                    if ((int)resp.StatusCode == 429)
                    {
                        Logger.Error("TumblrBlogCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                        shellService.ShowError(webException, Resources.LimitExceeded, blog.Name);
                        blog.Online = true;
                        return;
                    }
                }
            }
        }

        public override async Task UpdateMetaInformationAsync()
        {
            try
            {
                if (blog.Online)
                {
                    string document = await GetSvcPageAsync("1", "0");
                    var response = ConvertJsonToClass<TumblrJson>(document);

                    if (response.meta.status == 200)
                    {
                        blog.Title = response.response.posts.FirstOrDefault().blog.title;
                        blog.Description = response.response.posts.FirstOrDefault().blog.description;
                    }
                }
            }
            catch (WebException webException)
            {
                var webRespStatusCode = (int)((HttpWebResponse)webException?.Response).StatusCode;
                if (webRespStatusCode == 503)
                {
                    Logger.Error("TumblrBlogCrawler:GetUrlsAsync: {0}", "User not logged in");
                    shellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, blog.Name);
                }
            }
        }

        public async Task Crawl()
        {
            Logger.Verbose("TumblrBlogCrawler.Crawl:Start");

            Task grabber = GetUrlsAsync();
            Task<bool> download = downloader.DownloadBlogAsync();

            Task crawlerDownloader = Task.CompletedTask;
            if (blog.DumpCrawlerData)
                crawlerDownloader = crawlerDataDownloader.DownloadCrawlerDataAsync();

            await grabber;

            UpdateProgressQueueInformation(Resources.ProgressUniqueDownloads);
            blog.DuplicatePhotos = DetermineDuplicates<PhotoPost>();
            blog.DuplicateVideos = DetermineDuplicates<VideoPost>();
            blog.DuplicateAudios = DetermineDuplicates<AudioPost>();
            blog.TotalCount = (blog.TotalCount - blog.DuplicatePhotos - blog.DuplicateAudios - blog.DuplicateVideos);

            CleanCollectedBlogStatistics();

            await crawlerDownloader;
            await download;

            if (!ct.IsCancellationRequested)
            {
                blog.LastCompleteCrawl = DateTime.Now;
            }

            blog.Save();

            UpdateProgressQueueInformation("");
        }

        private async Task UpdateAuthentication()
        {
            string document = await ThrottleAsync(Authenticate).TimeoutAfter(shellService.Settings.TimeOut);
            authentication = ExtractAuthenticationKey(document);
            await UpdateCookieWithAuthentication().TimeoutAfter(shellService.Settings.TimeOut);
        }

        private async Task<T> ThrottleAsync<T>(Func<Task<T>> method)
        {
            if (shellService.Settings.LimitConnections)
            {
                return await method();
            }
            return await method();
        }

        protected async Task<string> Authenticate()
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                string url = "https://www.tumblr.com/blog_auth/" + blog.Name;
                var headers = new Dictionary<string, string>();
                HttpWebRequest request = webRequestFactory.CreatePostReqeust(url, url, headers);
                cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
                cookieService.GetUriCookie(request.CookieContainer, new Uri("https://" + blog.Name.Replace("+", "-") + ".tumblr.com"));
                string requestBody = "password=" + blog.Password;
                using (Stream postStream = await request.GetRequestStreamAsync())
                {
                    byte[] postBytes = Encoding.ASCII.GetBytes(requestBody);
                    await postStream.WriteAsync(postBytes, 0, postBytes.Length);
                    await postStream.FlushAsync();
                }

                requestRegistration = ct.Register(() => request.Abort());
                return await webRequestFactory.ReadReqestToEnd(request).TimeoutAfter(shellService.Settings.TimeOut);
            }
            finally
            {
                requestRegistration.Dispose();
            }
        }

        private static string ExtractAuthenticationKey(string document)
        {
            return Regex.Match(document, "name=\"auth\" value=\"([\\S]*)\"").Groups[1].Value;
        }

        protected async Task UpdateCookieWithAuthentication()
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                string url = "https://" + blog.Name + ".tumblr.com/";
                string referer = "https://www.tumblr.com/blog_auth/" + blog.Name;
                var headers = new Dictionary<string, string>();
                headers.Add("DNT", "1");
                HttpWebRequest request = webRequestFactory.CreatePostReqeust(url, referer, headers);
                cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
                cookieService.GetUriCookie(request.CookieContainer, new Uri("https://" + blog.Name.Replace("+", "-") + ".tumblr.com"));
                string requestBody = "auth=" + authentication;
                using (Stream postStream = await request.GetRequestStreamAsync())
                {
                    byte[] postBytes = Encoding.ASCII.GetBytes(requestBody);
                    await postStream.WriteAsync(postBytes, 0, postBytes.Length);
                    await postStream.FlushAsync();
                }

                requestRegistration = ct.Register(() => request.Abort());
                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    cookieService.SetUriCookie(response.Cookies);
                }
            }
            finally
            {
                requestRegistration.Dispose();
            }
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

        private async Task<ulong> GetHighestPostId()
        {
            string document = await GetSvcPageAsync("1", "0");
            var response = ConvertJsonToClass<TumblrJson>(document);

            ulong highestId;
            ulong.TryParse(blog.Title = response.response.posts.FirstOrDefault().id, out highestId);
            return highestId;
        }

        private static bool CheckPostAge(TumblrJson document, ulong lastId)
        {
            ulong highestPostId = 0;
            ulong.TryParse(document.response.posts.FirstOrDefault().id,
                out highestPostId);

            if (highestPostId < lastId)
            {
                return false;
            }
            return true;
        }

        private async Task GetUrlsAsync()
        {
            var semaphoreSlim = new SemaphoreSlim(shellService.Settings.ConcurrentScans);
            var trackedTasks = new List<Task>();

            if (!await CheckIfLoggedIn())
            {
                Logger.Error("TumblrBlogCrawler:GetUrlsAsync: {0}", "User not logged in");
                shellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, blog.Name);
                producerConsumerCollection.CompleteAdding();
                return;
            }

            foreach (int crawlerNumber in Enumerable.Range(0, shellService.Settings.ConcurrentScans))
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
                        string document = await GetSvcPageAsync(blog.PageSize.ToString(), (blog.PageSize * crawlerNumber).ToString());
                        var response = ConvertJsonToClass<TumblrJson>(document);
                        await AddUrlsToDownloadList(response, crawlerNumber);
                    }
                    catch (WebException webException) when ((webException.Response != null))
                    {
                        var resp = (HttpWebResponse)webException.Response;
                        if ((int)resp.StatusCode == 429)
                        {
                            // TODO: add retry logic?
                            Logger.Error("TumblrBlogCrawler:GetUrls:WebException {0}", webException);
                            shellService.ShowError(webException, Resources.LimitExceeded, blog.Name);
                        }
                    }
                    catch (TimeoutException timeoutException)
                    {
                        Logger.Error("TumblrBlogCrawler:GetUrls:WebException {0}", timeoutException);
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
            producerConsumerCollection.CompleteAdding();

            UpdateBlogStats();
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

        private async Task<bool> CheckIfLoggedIn()
        {
            try
            {
                string document = await GetSvcPageAsync(blog.PageSize.ToString(), (blog.PageSize * 1).ToString());
            }
            catch (WebException webException)
            {
                if (webException.Message.Contains("503"))
                {
                    return false;
                }
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
                HttpWebRequest request = webRequestFactory.CreateGetXhrReqeust(url, referer);
                cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
                cookieService.GetUriCookie(request.CookieContainer, new Uri("https://" + blog.Name.Replace("+", "-") + ".tumblr.com"));
                requestRegistration = ct.Register(() => request.Abort());
                return await webRequestFactory.ReadReqestToEnd(request).TimeoutAfter(shellService.Settings.TimeOut);
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
                if (!response.response.posts.Any())
                {
                    return;
                }

                crawlerNumber += shellService.Settings.ConcurrentScans;
            }
        }

        private bool CheckIfDownloadRebloggedPosts(Post post)
        {
            if (!blog.DownloadRebloggedPosts)
            {
                if (post.reblogged_from_url == null)
                    return true;
                return false;
            }
            return true;
        }

        private void AddToJsonQueue(TumblrCrawlerJsonData addToList)
        {
            if (blog.DumpCrawlerData)
                jsonQueue.Add(addToList);
        }

        private void AddPhotoUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadPhoto)
            {
                foreach (Post post in document.response.posts)
                {
                    if (!PostWithinTimeSpan(post))
                        continue;
                    if (post.type == "photo" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
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
                    if (post.type != "photo" && !tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any())
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
                AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
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
                //AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
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
                    if (post.type == "video" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
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
                    if (post.type != "video" && !tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any())
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
            AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
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
                    //AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
                }
                else if (shellService.Settings.VideoSize == 480)
                {
                    AddToDownloadList(new VideoPost(
                        "https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + "_480.mp4",
                        post.id, post.timestamp.ToString()));
                    //AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
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
                    if (post.type == "audio" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string audioUrl = post.audio_url;
                            if (!audioUrl.EndsWith(".mp3"))
                                audioUrl = audioUrl + ".mp3";
                            AddToDownloadList(new AudioPost(audioUrl, postId, post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(audioUrl.Split('/').Last(), ".json"), post));
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
                    if (post.type == "text" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParseText(post);
                            AddToDownloadList(new TextPost(textBody, postId, post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(postId, ".json"), post));
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
                    if (post.type == "quote" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParseQuote(post);
                            AddToDownloadList(new QuotePost(textBody, postId, post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(postId, ".json"), post));
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
                    if (post.type == "link" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParseLink(post);
                            AddToDownloadList(new LinkPost(textBody, postId, post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(postId, ".json"), post));
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
                    if (post.type == "chat" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParseConversation(post);
                            AddToDownloadList(new ConversationPost(textBody, postId, post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(postId, ".json"), post));
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
                    if (post.type == "answer" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParseAnswer(post);
                            AddToDownloadList(new AnswerPost(textBody, postId, post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(postId, ".json"), post));
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
                    if (post.type == "photo" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParsePhotoMeta(post);
                            AddToDownloadList(new PhotoMetaPost(textBody, postId));
                            AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(postId, ".json"), post));
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
                    if (post.type == "video" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParseVideoMeta(post);
                            AddToDownloadList(new VideoMetaPost(textBody, postId));
                            AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(postId, ".json"), post));
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
                    if (post.type == "audio" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParseAudioMeta(post);
                            AddToDownloadList(new AudioMetaPost(textBody, postId));
                            AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(postId, ".json"), post));
                        }
                    }
                }
            }
        }

        private async Task AddExternalPhotoUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadImgur)
            {
                await DownloadImgur(document);
            }
            if (blog.DownloadGfycat)
            {
                await DownloadGfycat(document);
            }
            if (blog.DownloadWebmshare)
            {
                Downloadwebmshare(document);
            }
        }

        private async Task DownloadImgur(TumblrJson document)
        {
            foreach (Post post in document.response.posts)
            {
                if (!PostWithinTimeSpan(post))
                    continue;
                if (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any())
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
                            AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
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
                                AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
                            }
                        }
                    }
                }
            }
        }

        private async Task DownloadGfycat(TumblrJson document)
        {
            foreach (Post post in document.response.posts)
            {
                if (!PostWithinTimeSpan(post))
                    continue;
                if (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any())
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
                            AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
                        }
                    }
                }
            }
        }

        private void Downloadwebmshare(TumblrJson document)
        {
            foreach (Post post in document.response.posts)
            {
                if (!PostWithinTimeSpan(post))
                    continue;
                if (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any())
                {
                    if (CheckIfDownloadRebloggedPosts(post))
                    {
                        var regex = webmshareParser.GetWebmshareUrlRegex();
                        foreach (Match match in regex.Matches(post.caption))
                        {
                            string webmshareId = match.Groups[2].Value;
                            string imageUrl = webmshareParser.CreateWebmshareUrl(webmshareId, blog.WebmshareType);
                            if (blog.SkipGif && (imageUrl.EndsWith(".gif") || imageUrl.EndsWith(".gifv")))
                            {
                                continue;
                            }
                            // TODO: postID
                            AddToDownloadList(new VideoPost(imageUrl, webmshareId,
                                post.timestamp.ToString()));
                            AddToJsonQueue(new TumblrCrawlerJsonData(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
                        }
                    }
                }
            }
        }

        private static string ParseText(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Title, post.title) +
                   Environment.NewLine + post.body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseQuote(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.text) +
                   Environment.NewLine + post.body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseLink(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Link, post.caption) +
                   Environment.NewLine + post.body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseConversation(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.dialogue.Select(dialogue => new { dialogue.name, dialogue.phrase })) +
                   Environment.NewLine + post.body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseAnswer(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   post.question +
                   Environment.NewLine +
                   post.answer +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        private static string ParsePhotoMeta(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoUrl, post.photos.Select(photo => photo.original_size.url).FirstOrDefault()) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoCaption, post.trail.Select(trail => trail.content_raw).FirstOrDefault()) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseVideoMeta(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseAudioMeta(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Summary, post.summary) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.AudioCaption, post.trail.Select(trail => trail.content_raw).FirstOrDefault()) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Artist, post.artist) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Title, post.title) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.track) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Album, post.album) +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.year) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }
    }
}
