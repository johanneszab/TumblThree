﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrApiJson;
using TumblThree.Applications.DataModels.TumblrCrawlerData;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Parser;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(TumblrBlog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TumblrBlogCrawler : AbstractTumblrCrawler, ICrawler
    {
        private readonly IDownloader downloader;
        private readonly ITumblrToTextParser<Post> tumblrJsonParser;
        private readonly IPostQueue<TumblrCrawlerData<Post>> jsonQueue;
        private readonly ICrawlerDataDownloader crawlerDataDownloader;

        private bool completeGrab = true;
        private bool incompleteCrawl = false;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        public TumblrBlogCrawler(IShellService shellService, CancellationToken ct, PauseToken pt,
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
                await GetApiPageWithRetryAsync(0);
                blog.Online = true;
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                    return;

                if (HandleUnauthorizedWebException(webException))
                    blog.Online = true;
                else if (HandleLimitExceededWebException(webException))
                    blog.Online = true;
                else
                {
                    Logger.Error("TumblrBlogCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                    shellService.ShowError(webException, Resources.BlogIsOffline, blog.Name);
                    blog.Online = false;
                }
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
                await UpdateMetaInformationCoreAsync();
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                    return;

                HandleLimitExceededWebException(webException);
            }
        }

        private async Task UpdateMetaInformationCoreAsync()
        {
            if (!blog.Online)
            {
                return;
            }

            string document = await GetApiPageWithRetryAsync(0);
            var response = ConvertJsonToClass<TumblrApiJson>(document);

            blog.Title = response.TumbleLog?.Title;
            blog.Description = response.TumbleLog?.Description;
            blog.TotalCount = response.PostsTotal;
        }

        public async Task CrawlAsync()
        {
            Logger.Verbose("TumblrBlogCrawler.Crawl:Start");

            ulong highestId = await GetHighestPostIdAsync();
            Task<bool> grabber = GetUrlsAsync();

            // FIXME: refactor downloader out of class
            Task<bool> download = downloader.DownloadBlogAsync();

            Task crawlerDownloader = Task.CompletedTask;
            if (blog.DumpCrawlerData)
                crawlerDownloader = crawlerDataDownloader.DownloadCrawlerDataAsync();

            bool apiLimitHit = await grabber;

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
                    blog.LastId = highestId;
                }
            }

            blog.Save();

            UpdateProgressQueueInformation("");
        }

        public override T ConvertJsonToClass<T>(string json)
        {
            if (json.Contains("tumblr_api_read"))
            {
                int jsonStart = json.IndexOf("{", StringComparison.Ordinal);
                json = json.Substring(jsonStart);
                json = json.Remove(json.Length - 2);
            }

            return base.ConvertJsonToClass<T>(json);
        }

        private static string GetApiUrl(string url, int count, int start = 0)
        {
            if (url.Last() != '/')
                url += "/";

            url += "api/read/json?debug=1&";

            var parameters = new Dictionary<string, string>
            {
                { "num", count.ToString() }
            };
            if (start > 0)
            {
                parameters["start"] = start.ToString();
            }

            return url + UrlEncode(parameters);
        }

        private async Task<string> GetApiPageAsync(int pageId)
        {
            string url = GetApiUrl(blog.Url, blog.PageSize, pageId * blog.PageSize);

            if (shellService.Settings.LimitConnections)
                crawlerService.Timeconstraint.Acquire();

            return await GetRequestAsync(url);
        }

        private async Task<string> GetApiPageWithRetryAsync(int pageId)
        {
            string page = string.Empty;
            var attemptCount = 0;

            do
            {
                page = await GetApiPageAsync(pageId);
                attemptCount++;
            }
            while (string.IsNullOrEmpty(page) && (attemptCount < shellService.Settings.MaxNumberOfRetries));

            return page;
        }

        private async Task UpdateTotalPostCountAsync()
        {
            try
            {
                await UpdateTotalPostCountCoreAsync();
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)             
                    return;
                
                HandleLimitExceededWebException(webException);
                blog.Posts = 0;
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
                blog.Posts = 0;
            }
        }

        private async Task UpdateTotalPostCountCoreAsync()
        {
            string document = await GetApiPageWithRetryAsync(0);
            var response = ConvertJsonToClass<TumblrApiJson>(document);
            int totalPosts = response.PostsTotal;
            blog.Posts = totalPosts;
        }

        private async Task<ulong> GetHighestPostIdAsync()
        {
            try
            {
                return await GetHighestPostIdCoreAsync();
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                    return 0;

                HandleLimitExceededWebException(webException);
                return 0;
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
                return 0;
            }
        }

        private async Task<ulong> GetHighestPostIdCoreAsync()
        {
            string document = await GetApiPageWithRetryAsync(0);
            var response = ConvertJsonToClass<TumblrApiJson>(document);

            ulong highestId;
            ulong.TryParse(response.Posts?.FirstOrDefault()?.Id, out highestId);
            return highestId;
        }

        protected override IEnumerable<int> GetPageNumbers()
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

        private async Task<bool> GetUrlsAsync()
        {
            trackedTasks = new List<Task>();
            semaphoreSlim = new SemaphoreSlim(shellService.Settings.ConcurrentScans);

            GenerateTags();

            await UpdateTotalPostCountAsync();

            foreach (int pageNumber in GetPageNumbers())
            {
                await semaphoreSlim.WaitAsync();

                if (!completeGrab)
                {
                    break;
                }

                if (CheckIfShouldStop())
                    break;

                CheckIfShouldPause();

                trackedTasks.Add(CrawlPageAsync(pageNumber));
            }

            await Task.WhenAll(trackedTasks);

            postQueue.CompleteAdding();
            jsonQueue.CompleteAdding();

            UpdateBlogStats();

            return incompleteCrawl;
        }

        private async Task CrawlPageAsync(int pageNumber)
        {
            try
            {
                string document = await GetApiPageWithRetryAsync(pageNumber);
                var response = ConvertJsonToClass<TumblrApiJson>(document);

                completeGrab = CheckPostAge(response);

                await AddUrlsToDownloadListAsync(response);

                numberOfPagesCrawled += blog.PageSize;
                UpdateProgressQueueInformation(Resources.ProgressGetUrlLong, numberOfPagesCrawled, blog.Posts);
            }
            catch (WebException webException) when (webException.Response != null)
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
            postTime = post.UnixTimestamp;
            return downloadFromUnixTime < postTime && postTime < downloadToUnixTime;
        }

        private bool CheckPostAge(TumblrApiJson response)
        {
            ulong highestPostId = 0;
            ulong.TryParse(response.Posts.FirstOrDefault().Id,
                out highestPostId);

            return highestPostId >= GetLastPostId();
        }

        private void AddToJsonQueue(TumblrCrawlerData<Post> addToList)
        {
            if (blog.DumpCrawlerData)
                jsonQueue.Add(addToList);
        }

        private async Task AddUrlsToDownloadListAsync(TumblrApiJson document)
        {
            foreach (Post post in document.Posts)
            {
                if (!PostWithinTimeSpan(post))
                    continue;
                if (!CheckIfContainsTaggedPost(post))
                    continue;
                if (!CheckIfDownloadRebloggedPosts(post))
                    continue;

                try
                {
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
                    await AddExternalPhotoUrlToDownloadListAsync(post);
                }
                catch (NullReferenceException)
                {
                }
            }
        }

        private bool CheckIfDownloadRebloggedPosts(Post post)
        {
            return blog.DownloadRebloggedPosts || !post.RebloggedFromUrl.Any();
        }

        private bool CheckIfContainsTaggedPost(Post post)
        {
            return !tags.Any() || post.Tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase));
        }

        private void AddPhotoUrlToDownloadList(Post post)
        {
            if (!blog.DownloadPhoto)
                return;

            if (post.Type == "photo")
            {
                AddPhotoUrl(post);
                AddPhotoSetUrl(post);
            }

            AddInlinePhotoUrl(post);

            if (blog.RegExPhotos)
                AddGenericInlinePhotoUrl(post);
        }

        private void AddVideoUrlToDownloadList(Post post)
        {
            if (!blog.DownloadVideo)
                return;

            Post postCopy = post;
            if (post.Type == "video")
            {
                AddVideoUrl(post);

                postCopy = (Post)post.Clone();
                postCopy.VideoPlayer = string.Empty;
            }

            //var videoUrls = new HashSet<string>();

            //var postCopy = (Post)post.Clone();
            AddInlineVideoUrl(post);
            AddInlineTumblrVideoUrl(InlineSearch(post), tumblrParser.GetTumblrVVideoUrlRegex());
            if (blog.RegExVideos)
                AddGenericInlineVideoUrl(post);

            //AddInlineVideoUrlsToDownloader(videoUrls, post);
        }

        private void AddAudioUrlToDownloadList(Post post)
        {
            if (blog.DownloadAudio)
                if (post.Type == "audio")
                    AddAudioUrl(post);
        }

        private void AddTextUrlToDownloadList(Post post)
        {
            if (!blog.DownloadText)
                return;
            if (post.Type != "regular")
                return;

            string textBody = tumblrJsonParser.ParseText(post);
            AddToDownloadList(new TextPost(textBody, post.Id));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private void AddQuoteUrlToDownloadList(Post post)
        {
            if (!blog.DownloadQuote)
                return;
            if (post.Type != "quote")
                return;

            string textBody = tumblrJsonParser.ParseQuote(post);
            AddToDownloadList(new QuotePost(textBody, post.Id));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private void AddLinkUrlToDownloadList(Post post)
        {
            if (!blog.DownloadLink)
                return;
            if (post.Type != "link")
                return;

            string textBody = tumblrJsonParser.ParseLink(post);
            AddToDownloadList(new LinkPost(textBody, post.Id));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private void AddConversationUrlToDownloadList(Post post)
        {
            if (!blog.DownloadConversation)
                return;
            if (post.Type != "conversation")
                return;

            string textBody = tumblrJsonParser.ParseConversation(post);
            AddToDownloadList(new ConversationPost(textBody, post.Id));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private void AddAnswerUrlToDownloadList(Post post)
        {
            if (!blog.DownloadAnswer)
                return;
            if (post.Type != "answer")
                return;

            string textBody = tumblrJsonParser.ParseAnswer(post);
            AddToDownloadList(new AnswerPost(textBody, post.Id));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private void AddPhotoMetaUrlToDownloadList(Post post)
        {
            if (!blog.CreatePhotoMeta)
                return;
            if (post.Type != "photo")
                return;

            string textBody = tumblrJsonParser.ParsePhotoMeta(post);
            AddToDownloadList(new PhotoMetaPost(textBody, post.Id));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private void AddVideoMetaUrlToDownloadList(Post post)
        {
            if (!blog.CreateVideoMeta)
                return;
            if (post.Type != "video")
                return;

            string textBody = tumblrJsonParser.ParseVideoMeta(post);
            AddToDownloadList(new VideoMetaPost(textBody, post.Id));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private void AddAudioMetaUrlToDownloadList(Post post)
        {
            if (!blog.CreateAudioMeta)
                return;
            if (post.Type != "audio")
                return;

            string textBody = tumblrJsonParser.ParseAudioMeta(post);
            AddToDownloadList(new AudioMetaPost(textBody, post.Id));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private string ParseImageUrl(Post post)
        {
            return (string)post.GetType().GetProperty("photo_url_" + ImageSize()).GetValue(post, null) ?? post.PhotoUrl1280;
        }

        private string ParseImageUrl(Photo post)
        {
            return (string)post.GetType().GetProperty("photo_url_" + ImageSize()).GetValue(post, null) ?? post.PhotoUrl1280;
        }

        private static string InlineSearch(Post post)
        {
            return string.Join(" ", post.PhotoCaption, post.VideoCaption, post.AudioCaption,
                post.ConversationText, post.RegularBody, post.Answer, post.Photos.Select(photo => photo.Caption),
                post.Conversation.Select(conversation => conversation.Phrase));
        }

        private void AddInlinePhotoUrl(Post post)
        {
            AddTumblrPhotoUrl(InlineSearch(post));
        }

        private void AddInlineVideoUrl(Post post)
        {
            AddTumblrVideoUrl(InlineSearch(post));
        }

        private void AddGenericInlineVideoUrl(Post post)
        {
            AddGenericVideoUrl(InlineSearch(post));
        }

        private void AddInlineVideoUrlsToDownloader(HashSet<string> videoUrls, Post post)
        {
            foreach (string videoUrl in videoUrls)
            {
                AddToDownloadList(new VideoPost(videoUrl, post.Id, post.UnixTimestamp.ToString()));
            }
        }

        private void AddPhotoUrl(Post post)
        {
            string imageUrl = ParseImageUrl(post);
            if (CheckIfSkipGif(imageUrl))
                return;

            AddToDownloadList(new PhotoPost(imageUrl, post.Id, post.UnixTimestamp.ToString()));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
        }

        private void AddPhotoSetUrl(Post post)
        {
            if (!post.Photos.Any())
                return;

            foreach (string imageUrl in post.Photos.Select(ParseImageUrl).Where(imgUrl => !CheckIfSkipGif(imgUrl)))
            {
                AddToDownloadList(new PhotoPost(imageUrl, post.Id, post.UnixTimestamp.ToString()));
                AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
            }
        }

        private void AddGenericInlinePhotoUrl(Post post)
        {
            AddGenericPhotoUrl(InlineSearch(post));
        }

        private void AddVideoUrl(Post post)
        {
            string videoUrl = Regex.Match(post.VideoPlayer, "\"url\":\"([\\S]*/(tumblr_[\\S]*)_filmstrip[\\S]*)\"").Groups[2]
                                   .ToString();

            if (shellService.Settings.VideoSize == 480)
                videoUrl += "_480";

            AddToDownloadList(
                new VideoPost("https://vtt.tumblr.com/" + videoUrl + ".mp4", post.Id, post.UnixTimestamp.ToString()));
            AddToJsonQueue(new TumblrCrawlerData<Post>(videoUrl + ".json", post));
        }

        private void AddAudioUrl(Post post)
        {
            string audioUrl = Regex.Match(post.AudioEmbed, "audio_file=([\\S]*)\"").Groups[1].ToString();
            audioUrl = HttpUtility.UrlDecode(audioUrl);
            if (!audioUrl.EndsWith(".mp3"))
                audioUrl = audioUrl + ".mp3";

            AddToDownloadList(new AudioPost(WebUtility.UrlDecode(audioUrl), post.Id, post.UnixTimestamp.ToString()));
            AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(audioUrl.Split('/').Last(), ".json"), post));
        }

        private async Task AddExternalPhotoUrlToDownloadListAsync(Post post)
        {
            string searchableText = InlineSearch(post);
            string timestamp = post.UnixTimestamp.ToString();

            if (blog.DownloadImgur) AddImgurUrl(searchableText, timestamp);

            if (blog.DownloadImgur) await AddImgurAlbumUrlAsync(searchableText, timestamp);

            if (blog.DownloadGfycat) await AddGfycatUrlAsync(searchableText, timestamp);

            if (blog.DownloadWebmshare) AddWebmshareUrl(searchableText, timestamp);

            if (blog.DownloadMixtape) AddMixtapeUrl(searchableText, timestamp);

            if (blog.DownloadUguu) AddUguuUrl(searchableText, timestamp);

            if (blog.DownloadSafeMoe) AddSafeMoeUrl(searchableText, timestamp);

            if (blog.DownloadLoliSafe) AddLoliSafeUrl(searchableText, timestamp);

            if (blog.DownloadCatBox) AddCatBoxUrl(searchableText, timestamp);
        }
    }
}
