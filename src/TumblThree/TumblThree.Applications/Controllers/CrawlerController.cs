using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Controllers
{
    [Export]
    internal class CrawlerController
    {
        private readonly IShellService shellService;
        private readonly IManagerService managerService;
        private readonly ICrawlerService crawlerService;
        private readonly Lazy<CrawlerViewModel> crawlerViewModel;
        private readonly DelegateCommand crawlCommand;
        private readonly DelegateCommand pauseCommand;
        private readonly DelegateCommand resumeCommand;
        private readonly DelegateCommand stopCommand;
        private CancellationTokenSource crawlBlogsCancellation;
        private PauseTokenSource crawlBlogsPause;
        private readonly List<Task> runningTasks;
        private readonly object lockObject;


        [ImportingConstructor]
        public CrawlerController(IShellService shellService, IManagerService managerService, ICrawlerService crawlerService,
            IDownloaderFactory downloaderFactory, Lazy<CrawlerViewModel> crawlerViewModel)
        {
            this.shellService = shellService;
            this.managerService = managerService;
            this.crawlerService = crawlerService;
            this.crawlerViewModel = crawlerViewModel;
            this.DownloaderFactory = downloaderFactory;
            this.crawlCommand = new DelegateCommand(Crawl, CanCrawl);
            this.pauseCommand = new DelegateCommand(Pause, CanPause);
            this.resumeCommand = new DelegateCommand(Resume, CanResume);
            this.stopCommand = new DelegateCommand(Stop, CanStop);
            this.runningTasks = new List<Task>();
            this.lockObject = new object();
        }

        private CrawlerViewModel CrawlerViewModel { get { return crawlerViewModel.Value; } }

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
                    stopCommand.Execute(null);
                Task.WaitAll(runningTasks.ToArray());
            }
            catch (System.AggregateException)
            {
            }
            foreach (Blog blog in managerService.BlogFiles)
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

        public void Stop()
        {
            if (resumeCommand.CanExecute(null))
                resumeCommand.Execute(null);

            crawlBlogsCancellation.Cancel();
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

        public void Pause()
        {
            crawlBlogsPause.PauseWithResponseAsync().Wait();
            crawlerService.IsPaused = true;
            pauseCommand.RaiseCanExecuteChanged();
            resumeCommand.RaiseCanExecuteChanged();
        }

        private bool CanResume()
        {
            return crawlerService.IsCrawl && crawlerService.IsPaused;
        }

        public void Resume()
        {
            crawlBlogsPause.Resume();
            crawlerService.IsPaused = false;
            pauseCommand.RaiseCanExecuteChanged();
            resumeCommand.RaiseCanExecuteChanged();
        }

        private bool CanCrawl()
        {
            return !crawlerService.IsCrawl;
        }

        private void Crawl()
        {
            var cancellation = new CancellationTokenSource();
            var pause = new PauseTokenSource();
            crawlBlogsCancellation = cancellation;
            crawlBlogsPause = pause;

            crawlerService.IsCrawl = true;

            crawlCommand.RaiseCanExecuteChanged();
            pauseCommand.RaiseCanExecuteChanged();
            stopCommand.RaiseCanExecuteChanged();

            for (int i = 0; i < shellService.Settings.ParallelBlogs; i++)
                runningTasks.Add(
                Task.Factory.StartNew(() => RunCrawlerTasks(cancellation.Token, pause.Token),
                    cancellation.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default).ContinueWith(task =>
                    {
                        runningTasks.Clear();
                    }));
        }

        private void RunCrawlerTasks(CancellationToken ct, PauseToken pt)
        {
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    throw new OperationCanceledException(ct);
                }

                if (pt.IsPaused)
                    pt.WaitWhilePausedWithResponseAsyc().Wait();

                Monitor.Enter(lockObject);
                if (crawlerService.ActiveItems.Count() < QueueManager.Items.Count())
                {
                    var queueList = QueueManager.Items.Except(crawlerService.ActiveItems);
                    var nextQueueItem = queueList.First();
                    var blog = nextQueueItem.Blog;

                    var downloader = DownloaderFactory.GetDownloader(blog.BlogType, shellService, crawlerService, blog);
                    downloader.IsBlogOnline().Wait();

                    if (crawlerService.ActiveItems.Any(item => item.Blog.Name.Contains(nextQueueItem.Blog.Name)))
                    {
                        QueueOnDispatcher.CheckBeginInvokeOnUI((Action)(() => QueueManager.RemoveItem(nextQueueItem)));
                        Monitor.Exit(lockObject);
                        continue;
                    }

                    if (!nextQueueItem.Blog.Online)
                    {
                        QueueOnDispatcher.CheckBeginInvokeOnUI((Action)(() => QueueManager.RemoveItem(nextQueueItem)));
                        Monitor.Exit(lockObject);
                        continue;
                    }

                    crawlerService.AddActiveItems(nextQueueItem);
                    Monitor.Exit(lockObject);
                    StartSiteSpecificDownloader(nextQueueItem, ct, pt);
                }
                else
                {
                    Monitor.Exit(lockObject);
                    Task.Delay(4000, ct).Wait();
                }
            }

        }

        private void StartSiteSpecificDownloader(QueueListItem queueListItem, CancellationToken ct, PauseToken pt)
        {

            var blog = queueListItem.Blog;
            blog.Dirty = true;
            var progress = SetupThrottledQueueListProgress(queueListItem);

            var downloader = DownloaderFactory.GetDownloader(blog.BlogType, shellService, crawlerService, blog);
            downloader.Crawl(progress, ct, pt);

            if (ct.IsCancellationRequested)
            {
                Monitor.Enter(lockObject);
                QueueOnDispatcher.CheckBeginInvokeOnUI((Action)(() => crawlerService.RemoveActiveItem(queueListItem)));
                Monitor.Exit(lockObject);
                throw new OperationCanceledException(ct);
            }
            else
            {
                Monitor.Enter(lockObject);
                QueueOnDispatcher.CheckBeginInvokeOnUI((Action)(() => QueueManager.RemoveItem(queueListItem)));
                QueueOnDispatcher.CheckBeginInvokeOnUI((Action)(() => crawlerService.RemoveActiveItem(queueListItem)));
                Monitor.Exit(lockObject);
            }
        }

        private ProgressThrottler<DataModels.DownloadProgress> SetupThrottledQueueListProgress(QueueListItem queueListItem)
        {
            var progressHandler = new Progress<DataModels.DownloadProgress>(value =>
            {
                queueListItem.Progress = value.Progress;
            });
            return new ProgressThrottler<DataModels.DownloadProgress>(progressHandler);
        }
    }
}
