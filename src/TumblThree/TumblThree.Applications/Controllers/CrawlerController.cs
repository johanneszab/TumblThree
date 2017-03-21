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
        private readonly ISelectionService selectionService;
        private readonly IEnvironmentService environmentService;
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
        public CrawlerController(IShellService shellService, IEnvironmentService environmentService, ISelectionService selectionService, ICrawlerService crawlerService,
            Lazy<CrawlerViewModel> crawlerViewModel)
        {
            this.shellService = shellService;
            this.environmentService = environmentService;
            this.selectionService = selectionService;
            this.crawlerService = crawlerService;
            this.crawlerViewModel = crawlerViewModel;
            this.crawlCommand = new DelegateCommand(Crawl, CanCrawl);
            this.pauseCommand = new DelegateCommand(Pause, CanPause);
            this.resumeCommand = new DelegateCommand(Resume, CanResume);
            this.stopCommand = new DelegateCommand(Stop, CanStop);
            this.runningTasks = new List<Task>();
            this.lockObject = new object();
        }

        private CrawlerViewModel CrawlerViewModel { get { return crawlerViewModel.Value; } }

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
            catch (System.AggregateException)
            {
            }
            foreach (Blog blog in selectionService.BlogFiles)
            {
                if (blog.Dirty)
                {
                    blog.Dirty = false;
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
                        if (task.Exception != null)
                        {
                            foreach (var innerEx in task.Exception.InnerExceptions)
                            {
                                Logger.Error("ManagerController:Crawl: {0}", innerEx);
                                //shellService.ShowError(innerEx, Resources.CrawlerError);
                            }
                        }
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
                if (selectionService.ActiveItems.Count() < QueueManager.Items.Count())
                {
                    var queueList = QueueManager.Items.Except(selectionService.ActiveItems);
                    var nextQueueItem = queueList.First();

                    Downloader.Downloader downloader = new Downloader.Downloader(shellService, nextQueueItem.Blog);
                    downloader.IsBlogOnline().Wait();

                    if (selectionService.ActiveItems.Any(item => item.Blog.Name.Contains(nextQueueItem.Blog.Name)))
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

                    selectionService.AddActiveItems(nextQueueItem);
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
            if (queueListItem.Blog is TumblrBlog)
            {

                var blog = (TumblrBlog)queueListItem.Blog;
                blog.Dirty = true;

                var progressHandler = new Progress<DataModels.DownloadProgress>(value =>
                {
                    queueListItem.Progress = value.Progress;
                });
                var progress = new ProgressThrottler<DataModels.DownloadProgress>(progressHandler);

                TumblrDownloader crawler = new TumblrDownloader(shellService, crawlerService, selectionService, blog);
                crawler.CrawlTumblrBlog(progress, ct, pt);

                if (ct.IsCancellationRequested)
                {
                    Monitor.Enter(lockObject);
                    QueueOnDispatcher.CheckBeginInvokeOnUI((Action)(() => selectionService.RemoveActiveItem(queueListItem)));
                    Monitor.Exit(lockObject);
                    throw new OperationCanceledException(ct);
                }
                else
                {
                    Monitor.Enter(lockObject);
                    QueueOnDispatcher.CheckBeginInvokeOnUI((Action)(() => QueueManager.RemoveItem(queueListItem)));
                    QueueOnDispatcher.CheckBeginInvokeOnUI((Action)(() => selectionService.RemoveActiveItem(queueListItem)));
                    Monitor.Exit(lockObject);
                }
            }
        }

    }
}
