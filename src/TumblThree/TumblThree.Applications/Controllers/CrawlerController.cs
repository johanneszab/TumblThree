using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Applications;

using TumblThree.Applications.Crawler;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Controllers
{
    [Export]
    internal class CrawlerController
    {
        private readonly ICrawlerFactory crawlerFactory;
        private readonly ICrawlerService crawlerService;
        private readonly Lazy<CrawlerViewModel> crawlerViewModel;
        private readonly IManagerService managerService;
        private readonly IShellService shellService;

        private readonly AsyncDelegateCommand crawlCommand;
        private readonly DelegateCommand pauseCommand;
        private readonly DelegateCommand resumeCommand;
        private readonly DelegateCommand stopCommand;

        private readonly object lockObject;
        private readonly List<Task> runningTasks;
        private CancellationTokenSource crawlerCancellationTokenSource;
        private PauseTokenSource crawlerPauseTokenSource;

        [ImportingConstructor]
        public CrawlerController(IShellService shellService, IManagerService managerService, ICrawlerService crawlerService,
            ICrawlerFactory crawlerFactory, Lazy<CrawlerViewModel> crawlerViewModel)
        {
            this.shellService = shellService;
            this.managerService = managerService;
            this.crawlerService = crawlerService;
            this.crawlerViewModel = crawlerViewModel;
            this.crawlerFactory = crawlerFactory;
            crawlCommand = new AsyncDelegateCommand(SetupCrawlAsync, CanCrawl);
            pauseCommand = new DelegateCommand(Pause, CanPause);
            resumeCommand = new DelegateCommand(Resume, CanResume);
            stopCommand = new DelegateCommand(Stop, CanStop);
            runningTasks = new List<Task>();
            lockObject = new object();
        }

        private CrawlerViewModel CrawlerViewModel => crawlerViewModel.Value;

        public QueueManager QueueManager { get; set; }

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
                    stopCommand.Execute(null);

                Task.WaitAll(runningTasks.ToArray());
            }
            catch (AggregateException)
            {
            }

            foreach (IBlog blog in managerService.BlogFiles)
            {
                if (blog.Dirty)
                    blog.Save();
            }
        }

        private bool CanStop() => crawlerService.IsCrawl;

        private void Stop()
        {
            if (resumeCommand.CanExecute(null))
                resumeCommand.Execute(null);

            crawlerCancellationTokenSource.Cancel();
            crawlerService.IsCrawl = false;
            crawlCommand.RaiseCanExecuteChanged();
            pauseCommand.RaiseCanExecuteChanged();
            resumeCommand.RaiseCanExecuteChanged();
            stopCommand.RaiseCanExecuteChanged();
        }

        private bool CanPause() => crawlerService.IsCrawl && !crawlerService.IsPaused;

        private void Pause()
        {
            crawlerPauseTokenSource.PauseWithResponseAsync().Wait();
            crawlerService.IsPaused = true;
            pauseCommand.RaiseCanExecuteChanged();
            resumeCommand.RaiseCanExecuteChanged();
        }

        private bool CanResume() => crawlerService.IsCrawl && crawlerService.IsPaused;

        private void Resume()
        {
            crawlerPauseTokenSource.Resume();
            crawlerService.IsPaused = false;
            pauseCommand.RaiseCanExecuteChanged();
            resumeCommand.RaiseCanExecuteChanged();
        }

        private bool CanCrawl() => !crawlerService.IsCrawl;

        private async Task SetupCrawlAsync()
        {
            crawlerCancellationTokenSource = new CancellationTokenSource();
            crawlerPauseTokenSource = new PauseTokenSource();

            crawlerService.IsCrawl = true;

            crawlCommand.RaiseCanExecuteChanged();
            pauseCommand.RaiseCanExecuteChanged();
            stopCommand.RaiseCanExecuteChanged();

            await Task.WhenAll(crawlerService.LibraryLoaded.Task, crawlerService.DatabasesLoaded.Task);

            for (var i = 0; i < shellService.Settings.ConcurrentBlogs; i++)
            {
                runningTasks.Add(Task.Run(() =>
                    RunCrawlerTasksAsync(crawlerCancellationTokenSource.Token, crawlerPauseTokenSource.Token)));
            }

            await CrawlAsync();
        }

        private async Task CrawlAsync()
        {
            try
            {
                await Task.WhenAll(runningTasks.ToArray());
            }
            catch
            {
            }
            finally
            {
                crawlerCancellationTokenSource.Dispose();
                runningTasks.Clear();
            }
        }

        private async Task RunCrawlerTasksAsync(CancellationToken ct, PauseToken pt)
        {
            while (true)
            {
                if (ct.IsCancellationRequested)
                    break;

                if (pt.IsPaused)
                    pt.WaitWhilePausedWithResponseAsyc().Wait();

                Monitor.Enter(lockObject);
                if (crawlerService.ActiveItems.Count < QueueManager.Items.Count)
                {
                    IEnumerable<QueueListItem> queueList = QueueManager.Items.Except(crawlerService.ActiveItems);
                    QueueListItem nextQueueItem = queueList.First();
                    IBlog blog = nextQueueItem.Blog;

                    ICrawler crawler = crawlerFactory.GetCrawler(blog, ct, pt, new Progress<DownloadProgress>());
                    crawler.IsBlogOnlineAsync().Wait(4000);

                    if (crawlerService.ActiveItems.Any(item =>
                        item.Blog.Name.Equals(nextQueueItem.Blog.Name) &&
                        item.Blog.BlogType.Equals(nextQueueItem.Blog.BlogType)))
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
                    await StartSiteSpecificDownloaderAsync(nextQueueItem, ct, pt);
                }
                else
                {
                    Monitor.Exit(lockObject);
                    await Task.Delay(4000, ct);
                }
            }
        }

        private async Task StartSiteSpecificDownloaderAsync(QueueListItem queueListItem, CancellationToken ct, PauseToken pt)
        {
            IBlog blog = queueListItem.Blog;
            blog.Dirty = true;
            ProgressThrottler<DownloadProgress> progress = SetupThrottledQueueListProgress(queueListItem);

            ICrawler crawler = crawlerFactory.GetCrawler(blog, ct, pt, progress);
            await crawler.CrawlAsync();

            Monitor.Enter(lockObject);
            QueueOnDispatcher.CheckBeginInvokeOnUI(() => crawlerService.RemoveActiveItem(queueListItem));
            Monitor.Exit(lockObject);

            if (!ct.IsCancellationRequested)
            {
                Monitor.Enter(lockObject);
                QueueOnDispatcher.CheckBeginInvokeOnUI(() => QueueManager.RemoveItem(queueListItem));
                Monitor.Exit(lockObject);
            }
        }

        private ProgressThrottler<DownloadProgress> SetupThrottledQueueListProgress(QueueListItem queueListItem)
        {
            var progressHandler = new Progress<DownloadProgress>(value => { queueListItem.Progress = value.Progress; });
            return new ProgressThrottler<DownloadProgress>(progressHandler, shellService.Settings.ProgressUpdateInterval);
        }
    }
}
