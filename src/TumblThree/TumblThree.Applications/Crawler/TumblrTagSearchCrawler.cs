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
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(TumblrTagSearchBlog))]
    public class TumblrTagSearchCrawler : AbstractTumblrCrawler, ICrawler
    {
        private readonly IDownloader downloader;
        private readonly PauseToken pt;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        public TumblrTagSearchCrawler(IShellService shellService, CancellationToken ct, PauseToken pt,
            IProgress<DownloadProgress> progress, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IDownloader downloader, IPostQueue<TumblrPost> postQueue, IBlog blog)
            : base(shellService, crawlerService, ct, progress, webRequestFactory, cookieService, postQueue, blog)
        {
            this.downloader = downloader;
            this.pt = pt;
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

                trackedTasks.Add(new Func<Task>(async () => { await CrawlPage(pageNumber, crawlerTimeOffset); })());
            }

            await Task.WhenAll(trackedTasks);

            postQueue.CompleteAdding();

            UpdateBlogStats();
        }

        private async Task CrawlPage(int pageNumber, long crawlerTimeOffset)
        {
            try
            {
                long pagination = DateTimeOffset.Now.ToUnixTimeSeconds() - (pageNumber * crawlerTimeOffset);
                long nextCrawlersPagination =
                    DateTimeOffset.Now.ToUnixTimeSeconds() - ((pageNumber + 1) * crawlerTimeOffset);
                await AddUrlsToDownloadList(pagination, nextCrawlersPagination);
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
            if (!shellService.Settings.LimitConnections)
                return await GetRequestAsync("https://www.tumblr.com/tagged/" + blog.Name + "?before=" + pagination);

            //crawlerService.Timeconstraint.Acquire();
            return await GetRequestAsync("https://www.tumblr.com/tagged/" + blog.Name + "?before=" + pagination);

            //string url = "https://www.tumblr.com/tagged/" + blog.Name + "?before=" + pagination;
            //return await ThrottleConnectionAsync(url, GetRequestAsync);
        }

        private async Task AddUrlsToDownloadList(long pagination, long nextCrawlersPagination)
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
                //if (!string.IsNullOrEmpty(blog.DownloadPages))
                //    return;
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
            var regex = new Regex("src=\"(http[A-Za-z0-9_/:.]*media.tumblr.com[A-Za-z0-9_/:.]*(jpg|png|gif))\"");
            foreach (Match match in regex.Matches(document))
            {
                string imageUrl = match.Groups[1].Value;
                if (imageUrl.Contains("avatar") || imageUrl.Contains("previews"))
                    continue;
                if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                {
                    continue;
                }

                imageUrl = ResizeTumblrImageUrl(imageUrl);
                // TODO: postID
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
                // TODO: postId
                if (shellService.Settings.VideoSize == 1080)
                {
                    // TODO: postID
                    AddToDownloadList(new VideoPost("https://vtt.tumblr.com/" + videoUrl + ".mp4",
                        Guid.NewGuid().ToString("N")));
                }
                else if (shellService.Settings.VideoSize == 480)
                {
                    // TODO: postID
                    AddToDownloadList(new VideoPost(
                        "https://vtt.tumblr.com/" + videoUrl + "_480.mp4",
                        Guid.NewGuid().ToString("N")));
                }
            }
        }
    }
}
