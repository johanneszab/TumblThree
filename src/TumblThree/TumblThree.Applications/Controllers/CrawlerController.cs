using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Applications;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Controllers
{
    [Export]
    internal class CrawlerController
    {
        private readonly AsyncDelegateCommand crawlCommand;
        private readonly ICrawlerService crawlerService;
        private readonly Lazy<CrawlerViewModel> crawlerViewModel;
        private readonly object lockObject;
        private readonly IManagerService managerService;
        private readonly DelegateCommand pauseCommand;
        private readonly DelegateCommand resumeCommand;
        private readonly List<Task> runningTasks;
        private readonly IShellService shellService;
        private readonly DelegateCommand stopCommand;
        private CancellationTokenSource crawlerCancellationToken;
        private PauseTokenSource crawlerPauseToken;

        [ImportingConstructor]
        public CrawlerController(IShellService shellService, IManagerService managerService, ICrawlerService crawlerService,
            IDownloaderFactory downloaderFactory, Lazy<CrawlerViewModel> crawlerViewModel)
        {
            this.shellService = shellService;
            this.managerService = managerService;
            this.crawlerService = crawlerService;
            this.crawlerViewModel = crawlerViewModel;
            DownloaderFactory = downloaderFactory;
            crawlCommand = new AsyncDelegateCommand(Crawl, CanCrawl);
            pauseCommand = new DelegateCommand(Pause, CanPause);
            resumeCommand = new DelegateCommand(Resume, CanResume);
            stopCommand = new DelegateCommand(Stop, CanStop);
            runningTasks = new List<Task>();
            lockObject = new object();
        }

        private CrawlerViewModel CrawlerViewModel
        {
            get { return crawlerViewModel.Value; }
        }

        public QueueManager QueueManager { get; set; }

        public IDownloaderFactory DownloaderFactory { get; set; }

        public void Initialize()
        {
            crawlerService.CrawlCommand = crawlCommand;
            crawlerService.PauseCommand = pauseCommand;
            crawlerService.ResumeCommand = resumeCommand;
            crawlerService.StopCommand = stopCommand;
            shellService.CrawlerView = CrawlerViewModel.View;
        }

        public void Shutdown()
        {
            try
            {
                if (stopCommand.CanExecute(null))
                {
                    stopCommand.Execute(null);
                }
                Task.WaitAll(runningTasks.ToArray());
            }
            catch (AggregateException)
            {
            }
            foreach (IBlog blog in managerService.BlogFiles)
            {
                if (blog.Dirty)
                {
                    blog.Save();
                }
            }
        }

        private bool CanStop()
        {
            return crawlerService.IsCrawl;
        }

        private void Stop()
        {
            if (resumeCommand.CanExecute(null))
            {
                resumeCommand.Execute(null);
            }

            crawlerCancellationToken.Cancel();
            crawlerService.IsCrawl = false;
            crawlCommand.RaiseCanExecuteChanged();
            pauseCommand.RaiseCanExecuteChanged();
            resumeCommand.RaiseCanExecuteChanged();
            stopCommand.RaiseCanExecuteChanged();
        }

        private bool CanPause()
        {
            return crawlerService.IsCrawl && !crawlerService.IsPaused;
        }

        private void Pause()
        {
            crawlerPauseToken.PauseWithResponseAsync().Wait();
            crawlerService.IsPaused = true;
            pauseCommand.RaiseCanExecuteChanged();
            resumeCommand.RaiseCanExecuteChanged();
        }

        private bool CanResume()
        {
            return crawlerService.IsCrawl && crawlerService.IsPaused;
        }

        private void Resume()
        {
            crawlerPauseToken.Resume();
            crawlerService.IsPaused = false;
            pauseCommand.RaiseCanExecuteChanged();
            resumeCommand.RaiseCanExecuteChanged();
        }

        private bool CanCrawl()
        {
            return !crawlerService.IsCrawl;
        }

        private async Task Crawl()
        {
            var cancellation = new CancellationTokenSource();
            var pause = new PauseTokenSource();
            crawlerCancellationToken = cancellation;
            crawlerPauseToken = pause;

            crawlerService.IsCrawl = true;

            crawlCommand.RaiseCanExecuteChanged();
            pauseCommand.RaiseCanExecuteChanged();
            stopCommand.RaiseCanExecuteChanged();

            for (var i = 0; i < shellService.Settings.ParallelBlogs; i++)
            {
                runningTasks.Add(Task.Run(() => RunCrawlerTasks(cancellation.Token, pause.Token)));
            }

            try { await Task.WhenAll(runningTasks.ToArray()); crawlerCancellationToken.Dispose(); }
            catch { }
            finally { runningTasks.Clear(); }
        }

        private async Task RunCrawlerTasks(CancellationToken ct, PauseToken pt)
        {
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                if (pt.IsPaused)
                {
                    pt.WaitWhilePausedWithResponseAsyc().Wait();
                }

                Monitor.Enter(lockObject);
                if (crawlerService.ActiveItems.Count() < QueueManager.Items.Count())
                {
                    IEnumerable<QueueListItem> queueList = QueueManager.Items.Except(crawlerService.ActiveItems);
                    QueueListItem nextQueueItem = queueList.First();
                    IBlog blog = nextQueueItem.Blog;

                    IDownloader downloader = DownloaderFactory.GetDownloader(blog.BlogType, ct, pt, new Progress<DownloadProgress>(), shellService, crawlerService, blog);
                    downloader.IsBlogOnlineAsync().Wait(4000);

                    if (crawlerService.ActiveItems.Any(item => item.Blog.Name.Equals(nextQueueItem.Blog.Name)))
                    {
                        QueueOnDispatcher.CheckBeginInvokeOnUI(() => QueueManager.RemoveItem(nextQueueItem));
                        Monitor.Exit(lockObject);
                        continue;
                    }

                    if (!nextQueueItem.Blog.Online)
                    {
                        QueueOnDispatcher.CheckBeginInvokeOnUI(() => QueueManager.RemoveItem(nextQueueItem));
                        Monitor.Exit(lockObject);
                        continue;
                    }

                    crawlerService.AddActiveItems(nextQueueItem);
                    Monitor.Exit(lockObject);
                    await StartSiteSpecificDownloader(nextQueueItem, ct, pt);
                }
                else
                {
                    Monitor.Exit(lockObject);
                    await Task.Delay(4000, ct);
                }
            }
        }

        private async Task StartSiteSpecificDownloader(QueueListItem queueListItem, CancellationToken ct, PauseToken pt)
        {
            IBlog blog = queueListItem.Blog;
            blog.Dirty = true;
            ProgressThrottler<DownloadProgress> progress = SetupThrottledQueueListProgress(queueListItem);

            IDownloader downloader = DownloaderFactory.GetDownloader(blog.BlogType, ct, pt, progress, shellService, crawlerService, blog);
            await downloader.Crawl();

            if (ct.IsCancellationRequested)
            {
                Monitor.Enter(lockObject);
                QueueOnDispatcher.CheckBeginInvokeOnUI(() => crawlerService.RemoveActiveItem(queueListItem));
                Monitor.Exit(lockObject);
            }
            else
            {
                Monitor.Enter(lockObject);
                QueueOnDispatcher.CheckBeginInvokeOnUI(() => QueueManager.RemoveItem(queueListItem));
                QueueOnDispatcher.CheckBeginInvokeOnUI(() => crawlerService.RemoveActiveItem(queueListItem));
                Monitor.Exit(lockObject);
            }
        }

        private ProgressThrottler<DataModels.DownloadProgress> SetupThrottledQueueListProgress(QueueListItem queueListItem)
        {
            var progressHandler = new Progress<DataModels.DownloadProgress>(value => { queueListItem.Progress = value.Progress; });
            return new ProgressThrottler<DataModels.DownloadProgress>(progressHandler);
        }
    }
}
