using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
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
    [ExportMetadata("BlogType", typeof(TumblrLikedByBlog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TumblrLikedByCrawler : AbstractTumblrCrawler, ICrawler
    {
        private readonly IDownloader downloader;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        public TumblrLikedByCrawler(IShellService shellService, CancellationToken ct, PauseToken pt,
            IProgress<DownloadProgress> progress, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IDownloader downloader, IImgurParser imgurParser, IGfycatParser gfycatParser,
            IWebmshareParser webmshareParser, IMixtapeParser mixtapeParser, IUguuParser uguuParser, ISafeMoeParser safemoeParser,
            ILoliSafeParser lolisafeParser, ICatBoxParser catboxParser, IPostQueue<TumblrPost> postQueue, IBlog blog)
            : base(shellService, crawlerService, ct, pt, progress, webRequestFactory, cookieService, imgurParser, gfycatParser,
                webmshareParser, mixtapeParser, uguuParser, safemoeParser, lolisafeParser, catboxParser, postQueue, blog)
        {
            this.downloader = downloader;
        }

        public async Task CrawlAsync()
        {
            Logger.Verbose("TumblrLikedByCrawler.Crawl:Start");

            Task grabber = GetUrlsAsync();
            Task<bool> download = downloader.DownloadBlogAsync();

            await grabber;

            UpdateProgressQueueInformation(Resources.ProgressUniqueDownloads);
            blog.DuplicatePhotos = DetermineDuplicates<PhotoPost>();
            blog.DuplicateVideos = DetermineDuplicates<VideoPost>();
            blog.DuplicateAudios = DetermineDuplicates<AudioPost>();
            blog.TotalCount = (blog.TotalCount - blog.DuplicatePhotos - blog.DuplicateAudios - blog.DuplicateVideos);

            CleanCollectedBlogStatistics();

            await download;

            if (!ct.IsCancellationRequested)
            {
                blog.LastCompleteCrawl = DateTime.Now;
            }

            blog.Save();

            UpdateProgressQueueInformation("");
        }

        private async Task GetUrlsAsync()
        {
            semaphoreSlim = new SemaphoreSlim(shellService.Settings.ConcurrentScans);
            trackedTasks = new List<Task>();

            if (!await CheckIfLoggedInAsync())
            {
                Logger.Error("TumblrLikedByCrawler:GetUrlsAsync: {0}", "User not logged in");
                shellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, blog.Name);
                postQueue.CompleteAdding();
                return;
            }

            long pagination = CreateStartPagination();

            // TODO: find way to parallelize without losing content.
            foreach (int crawlerNumber in Enumerable.Range(0, 1))
            {
                await semaphoreSlim.WaitAsync();

                trackedTasks.Add(CrawlPage(pagination, crawlerNumber));
            }

            await Task.WhenAll(trackedTasks);

            postQueue.CompleteAdding();

            UpdateBlogStats();
        }

        private async Task CrawlPage(long pagination, int crawlerNumber)
        {
            try
            {
                await AddUrlsToDownloadList(pagination, crawlerNumber);
            }
            catch (TimeoutException timeoutException)
            {
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

        public override async Task IsBlogOnlineAsync()
        {
            try
            {
                await GetRequestAsync(blog.Url);
                blog.Online = true;
            }
            catch (WebException webException)
            {
                Logger.Error("TumblrLikedByCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                shellService.ShowError(webException, Resources.BlogIsOffline, blog.Name);
                blog.Online = false;
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.OnlineChecking);
                blog.Online = false;
            }
        }

        private long CreateStartPagination()
        {
            if (string.IsNullOrEmpty(blog.DownloadTo))
                return DateTimeOffset.Now.ToUnixTimeSeconds();

            DateTime downloadTo = DateTime.ParseExact(blog.DownloadTo, "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None);
            var dateTimeOffset = new DateTimeOffset(downloadTo);
            return dateTimeOffset.ToUnixTimeSeconds();
        }

        private bool CheckIfPagecountReached(int pageCount)
        {
            int numberOfPages = RangeToSequence(blog.DownloadPages).Count();
            return pageCount >= numberOfPages;
        }

        private async Task<bool> CheckIfLoggedInAsync()
        {
            try
            {
                string document = await GetRequestAsync(blog.Url + "/page/1");
                return !document.Contains("<div class=\"signup_view account login\"");
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
                return false;
            }
        }

        private async Task AddUrlsToDownloadList(long pagination, int crawlerNumber)
        {
            while (true)
            {
                if (CheckifShouldStop())
                    return;

                CheckIfShouldPause();

                string document = await GetRequestAsync(blog.Url + "/page/" + crawlerNumber + "/" + pagination);
                if (document.Contains("<div class=\"no_posts_found\""))
                {
                    return;
                }

                try
                {
                    AddPhotoUrlToDownloadList(document);
                    AddVideoUrlToDownloadList(document);
                }
                catch (NullReferenceException)
                {
                }

                Interlocked.Increment(ref numberOfPagesCrawled);
                UpdateProgressQueueInformation(Resources.ProgressGetUrlShort, numberOfPagesCrawled);
                pagination = ExtractNextPageLink(document);
                crawlerNumber++;
                if (!CheckIfWithinTimespan(pagination))
                    return;
            }
        }

        private static long ExtractNextPageLink(string document)
        {
            // Example pagination:
            //
            // <div id="pagination" class="pagination "><a id="previous_page_link" href="/liked/by/wallpaperfx/page/3/-1457140452" class="previous button chrome">Previous</a>
            // <a id="next_page_link" href="/liked/by/wallpaperfx/page/5/1457139681" class="next button chrome blue">Next</a></div></div>

            long unixTime = 0;
            var pagination = "(id=\"next_page_link\" href=\"[A-Za-z0-9_/:.-]+/([0-9]+)/([A-Za-z0-9]+))\"";
            long.TryParse(Regex.Match(document, pagination).Groups[3].Value, out unixTime);
            return unixTime;
        }

        private bool CheckIfWithinTimespan(long pagination)
        {
            if (string.IsNullOrEmpty(blog.DownloadFrom))
                return true;

            DateTime downloadFrom = DateTime.ParseExact(blog.DownloadFrom, "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None);
            var dateTimeOffset = new DateTimeOffset(downloadFrom);
            return pagination >= dateTimeOffset.ToUnixTimeSeconds();
        }

        private void AddPhotoUrlToDownloadList(string document)
        {
            if (!blog.DownloadPhoto)
                return;
            var regex = new Regex("src=\"(http[A-Za-z0-9_/:.]*media.tumblr.com[A-Za-z0-9_/:.]*(jpg|png|gif))\"");
            foreach (Match match in regex.Matches(document))
            {
                string imageUrl = match.Groups[1].Value;
                if (imageUrl.Contains("avatar") || imageUrl.Contains("previews"))
                    continue;
                if (CheckIfSkipGif(imageUrl))
                    continue;

                imageUrl = ResizeTumblrImageUrl(imageUrl);
                // TODO: add valid postID
                AddToDownloadList(new PhotoPost(imageUrl, Guid.NewGuid().ToString("N")));
            }
        }

        private void AddVideoUrlToDownloadList(string document)
        {
            if (!blog.DownloadVideo)
                return;
            var regex = new Regex("src=\"(http[A-Za-z0-9_/:.]*video_file[\\S]*/(tumblr_[\\w]*))[0-9/]*\"");
            foreach (Match match in regex.Matches(document))
            {
                string videoUrl = match.Groups[2].Value;
                // TODO: add valid postID
                if (shellService.Settings.VideoSize == 480)
                    videoUrl += "_480";

                // TODO: add valid postID
                AddToDownloadList(new VideoPost("https://vtt.tumblr.com/" + videoUrl + ".mp4",
                    Guid.NewGuid().ToString("N")));
            }
        }
    }
}
