using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
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
    [ExportMetadata("BlogType", typeof(TumblrTagSearchBlog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TumblrTagSearchCrawler : AbstractTumblrCrawler, ICrawler
    {
        private readonly IDownloader downloader;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        public TumblrTagSearchCrawler(IShellService shellService, CancellationToken ct, PauseToken pt,
            IProgress<DownloadProgress> progress, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IDownloader downloader, ITumblrParser tumblrParser, IImgurParser imgurParser,
            IGfycatParser gfycatParser, IWebmshareParser webmshareParser, IMixtapeParser mixtapeParser, IUguuParser uguuParser,
            ISafeMoeParser safemoeParser, ILoliSafeParser lolisafeParser, ICatBoxParser catboxParser,
            IPostQueue<TumblrPost> postQueue, IBlog blog)
            : base(shellService, crawlerService, ct, pt, progress, webRequestFactory, cookieService, tumblrParser, imgurParser,
                gfycatParser, webmshareParser, mixtapeParser, uguuParser, safemoeParser, lolisafeParser, catboxParser, postQueue,
                blog)
        {
            this.downloader = downloader;
        }

        public async Task CrawlAsync()
        {
            Logger.Verbose("TumblrTagSearchCrawler.Crawl:Start");

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
                Logger.Error("TumblrTagSearchCrawler:GetUrlsAsync: {0}", "User not logged in");
                shellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, blog.Name);
                postQueue.CompleteAdding();
                return;
            }

            GenerateTags();

            long crawlerTimeOffset = GenerateCrawlerTimeOffsets();

            foreach (int pageNumber in GetPageNumbers())
            {
                await semaphoreSlim.WaitAsync();

                trackedTasks.Add(CrawlPageAsync(pageNumber, crawlerTimeOffset));
            }

            await Task.WhenAll(trackedTasks);

            postQueue.CompleteAdding();

            UpdateBlogStats();
        }

        private async Task CrawlPageAsync(int pageNumber, long crawlerTimeOffset)
        {
            try
            {
                long pagination = DateTimeOffset.Now.ToUnixTimeSeconds() - (pageNumber * crawlerTimeOffset);
                long nextCrawlersPagination =
                    DateTimeOffset.Now.ToUnixTimeSeconds() - ((pageNumber + 1) * crawlerTimeOffset);
                await AddUrlsToDownloadListAsync(pagination, nextCrawlersPagination);
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

        private long GenerateCrawlerTimeOffsets()
        {
            long tagsIntroduced = 1178470824; // Unix time of 05/06/2007 @ 5:00pm (UTC)
            if (!string.IsNullOrEmpty(blog.DownloadFrom))
            {
                DateTime downloadFrom = DateTime.ParseExact(blog.DownloadFrom, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None);
                var dateTimeOffset = new DateTimeOffset(downloadFrom);
                tagsIntroduced = dateTimeOffset.ToUnixTimeSeconds();
            }

            long unixTimeNow = DateTimeOffset.Now.ToUnixTimeSeconds();
            if (!string.IsNullOrEmpty(blog.DownloadTo))
            {
                DateTime downloadTo = DateTime.ParseExact(blog.DownloadTo, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None);
                var dateTimeOffset = new DateTimeOffset(downloadTo);
                unixTimeNow = dateTimeOffset.ToUnixTimeSeconds();
            }

            long tagsLifeTime = unixTimeNow - tagsIntroduced;
            return tagsLifeTime / shellService.Settings.ConcurrentScans;
        }

        private async Task<bool> CheckIfLoggedInAsync()
        {
            try
            {
                string document = await GetTaggedSearchPageAsync(DateTimeOffset.Now.ToUnixTimeSeconds());
                return !document.Contains("SearchResultsModel");
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
                return false;
            }
        }

        private long ExtractNextPageLink(string document)
        {
            long unixTime = 0;
            string pagination = "id=\"next_page_link\" href=\"/tagged/" + Regex.Escape(blog.Name) + "\\?before=";
            long.TryParse(Regex.Match(document, pagination + "([\\d]*)\"").Groups[1].Value, out unixTime);
            return unixTime;
        }

        private async Task<string> GetTaggedSearchPageAsync(long pagination)
        {
            if (shellService.Settings.LimitConnections)
                crawlerService.Timeconstraint.Acquire();

            return await GetRequestAsync("https://www.tumblr.com/tagged/" + blog.Name + "?before=" + pagination);
        }

        private async Task AddUrlsToDownloadListAsync(long pagination, long nextCrawlersPagination)
        {
            while (true)
            {
                if (CheckifShouldStop())
                    return;

                CheckIfShouldPause();

                string document = await GetTaggedSearchPageAsync(pagination);
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

                if (pagination < nextCrawlersPagination)
                    return;
                if (!CheckIfWithinTimespan(pagination))
                    return;
            }
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
            AddTumblrPhotoUrl(document);
        }

        private void AddVideoUrlToDownloadList(string document)
        {
            if (!blog.DownloadVideo)
                return;
            AddTumblrVideoUrl(document);
        }
    }
}
