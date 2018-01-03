using System;
using System.Collections.Concurrent;
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
using TumblThree.Applications.Extensions;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(TumblrTagSearchBlog))]
    public class TumblrTagSearchCrawler : AbstractCrawler, ICrawler
    {
        private readonly IDownloader downloader;
        private readonly PauseToken pt;

        public TumblrTagSearchCrawler(IShellService shellService, CancellationToken ct, PauseToken pt,
            IProgress<DownloadProgress> progress, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory, ISharedCookieService cookieService, IDownloader downloader, IPostQueue<TumblrPost> postQueue, IBlog blog)
            : base(shellService, ct, progress, webRequestFactory, cookieService, postQueue, blog)
        {
            this.downloader = downloader;
            this.pt = pt;
        }

        public async Task Crawl()
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
            var semaphoreSlim = new SemaphoreSlim(shellService.Settings.ConcurrentScans);
            var trackedTasks = new List<Task>();

            if (!await CheckIfLoggedIn())
            {
                Logger.Error("TumblrTagSearchCrawler:GetUrlsAsync: {0}", "User not logged in");
                shellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, blog.Name);
                postQueue.CompleteAdding();
                return;
            }

            long crawlerTimeOffset = GenerateCrawlerTimeOffsets();

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
                        long pagination = DateTimeOffset.Now.ToUnixTimeSeconds() - (crawlerNumber * crawlerTimeOffset);
                        long nextCrawlersPagination = DateTimeOffset.Now.ToUnixTimeSeconds() - ((crawlerNumber + 1) * crawlerTimeOffset);
                        await AddUrlsToDownloadList(pagination, nextCrawlersPagination);
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

            postQueue.CompleteAdding();

            UpdateBlogStats();
        }

        private long GenerateCrawlerTimeOffsets()
        {
            long tagsIntroduced = 1178470824; // Unix time of 05/06/2007 @ 5:00pm (UTC)
            if (!string.IsNullOrEmpty(blog.DownloadFrom))
            {
                var downloadFrom = DateTime.ParseExact(blog.DownloadFrom, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None);
                var dateTimeOffset = new DateTimeOffset(downloadFrom);
                tagsIntroduced = dateTimeOffset.ToUnixTimeSeconds();
            }
            long unixTimeNow = DateTimeOffset.Now.ToUnixTimeSeconds();
            if (!string.IsNullOrEmpty(blog.DownloadTo))
            {
                var downloadTo = DateTime.ParseExact(blog.DownloadTo, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None);
                var dateTimeOffset = new DateTimeOffset(downloadTo);
                unixTimeNow = dateTimeOffset.ToUnixTimeSeconds();
            }
            long tagsLifeTime = unixTimeNow - tagsIntroduced;
            return tagsLifeTime / shellService.Settings.ConcurrentScans;
        }

        private async Task<bool> CheckIfLoggedIn()
        {
            string document = await GetTaggedSearchPageAsync(DateTimeOffset.Now.ToUnixTimeSeconds());
            return !document.Contains("SearchResultsModel");
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
            {
                return await RequestGetAsync(pagination);
            }
            return await RequestGetAsync(pagination);
        }

        protected virtual async Task<string> RequestGetAsync(long pagination)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                string urlForGetRequest = "https://www.tumblr.com/tagged/" + blog.Name + "?before=" + pagination;
                HttpWebRequest request = webRequestFactory.CreateGetReqeust(urlForGetRequest);
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
            }
        }

        private bool CheckIfWithinTimespan(long pagination)
        {
            if (!string.IsNullOrEmpty(blog.DownloadFrom))
            {
                var downloadFrom = DateTime.ParseExact(blog.DownloadFrom, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None);
                var dateTimeOffset = new DateTimeOffset(downloadFrom);
                if (pagination < dateTimeOffset.ToUnixTimeSeconds())
                    return false;
            }
            return true;
        }

        private void AddPhotoUrlToDownloadList(string document)
        {
            if (blog.DownloadPhoto)
            {
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
        }

        private void AddVideoUrlToDownloadList(string document)
        {
            if (blog.DownloadVideo)
            {
                var regex = new Regex("src=\"(http[A-Za-z0-9_/:.]*.com/video_file/[A-Za-z0-9_/:.]*)\"");
                foreach (Match match in regex.Matches(document))
                {
                    string videoUrl = match.Groups[1].Value;
                    // TODO: postId
                    if (shellService.Settings.VideoSize == 1080)
                    {
                        // TODO: postID
                        AddToDownloadList(new VideoPost(videoUrl.Replace("/480", "") + ".mp4", Guid.NewGuid().ToString("N")));
                    }
                    else if (shellService.Settings.VideoSize == 480)
                    {
                        // TODO: postID
                        AddToDownloadList(new VideoPost(
                            "https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + "_480.mp4",
                            Guid.NewGuid().ToString("N")));
                    }
                }
            }
        }
    }
}
