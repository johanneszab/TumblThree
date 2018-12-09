using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
    [ExportMetadata("BlogType", typeof(TumblrBlog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TumblrBlogCrawler : AbstractTumblrCrawler, ICrawler
    {
        private readonly IDownloader downloader;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        public TumblrBlogCrawler(IShellService shellService, CancellationToken ct, PauseToken pt,
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

        public override async Task IsBlogOnlineAsync()
        {
            try
            {
                blog.Online = true;
                string document = await RequestDataAsync(blog.Url);
            }
            catch (WebException)
            {
                blog.Online = false;
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.OnlineChecking);
                blog.Online = false;
            }
        }

        private bool CheckIfPasswordProtecedBlog(string document)
        {
            if (!Regex.IsMatch(document, "<form id=\"auth_password\" method=\"post\">"))
                return false;

            Logger.Error("TumblrBlogCrawler:CheckIfPasswordProtecedBlog:PasswordProtectedBlog {0}",
                Resources.PasswordProtected, blog.Name);
            shellService.ShowError(new WebException(), Resources.PasswordProtected, blog.Name);
            return true;
        }

        public async Task CrawlAsync()
        {
            Logger.Verbose("TumblrBlogCrawler.CrawlAsync:Start");

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

            GenerateTags();

            foreach (int crawlerNumber in Enumerable.Range(1, shellService.Settings.ConcurrentScans))
            {
                await semaphoreSlim.WaitAsync();

                trackedTasks.Add(CrawlPageAsync(crawlerNumber));
            }

            await Task.WhenAll(trackedTasks);

            postQueue.CompleteAdding();

            UpdateBlogStats();
        }

        private async Task CrawlPageAsync(int crawlerNumber)
        {
            try
            {
                string document = await RequestDataAsync(blog.Url + "page/" + crawlerNumber);
                await AddUrlsToDownloadListAsync(document, crawlerNumber);
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

        private async Task AddUrlsToDownloadListAsync(string document, int crawlerNumber)
        {
            while (true)
            {
                if (CheckifShouldStop())
                    return;

                CheckIfShouldPause();

                AddPhotoUrlToDownloadList(document);
                AddVideoUrlToDownloadList(document);

                Interlocked.Increment(ref numberOfPagesCrawled);
                UpdateProgressQueueInformation(Resources.ProgressGetUrlShort, numberOfPagesCrawled);
                document = await RequestDataAsync(blog.Url + "page/" + crawlerNumber);
                if (!document.Contains((crawlerNumber + 1).ToString()))
                {
                    return;
                }

                crawlerNumber += shellService.Settings.ConcurrentScans;
            }
        }

        private void AddPhotoUrlToDownloadList(string document)
        {
            if (!blog.DownloadPhoto)
                return;

            AddTumblrPhotoUrl(document);

            if (blog.RegExPhotos)
                AddGenericPhotoUrl(document);
        }

        private void AddVideoUrlToDownloadList(string document)
        {
            if (!blog.DownloadVideo)
                return;

            AddTumblrVideoUrl(document);
            AddInlineTumblrVideoUrl(document, tumblrParser.GetTumblrVVideoUrlRegex());

            if (blog.RegExVideos)
                AddGenericVideoUrl(document);
        }
    }
}
