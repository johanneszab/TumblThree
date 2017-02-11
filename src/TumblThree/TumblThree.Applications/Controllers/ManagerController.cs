using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Waf.Applications;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using Datamodels = TumblThree.Applications.DataModels;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using System.ComponentModel;
using TumblThree.Applications.Properties;
using TumblThree.Domain.Queue;
using System.Windows;
using System.Windows.Threading;
using System.Globalization;
using System.Xml.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace TumblThree.Applications.Controllers
{
    [Export]
    internal class ManagerController
    {
        private readonly IShellService shellService;
        private readonly IEnvironmentService environmentService;
        private readonly SelectionService selectionService;
        private readonly CrawlerService crawlerService;
        private readonly Lazy<ManagerViewModel> managerViewModel;
        private readonly ObservableCollection<Blog> blogFiles;
        private readonly DelegateCommand addBlogCommand;
        private readonly DelegateCommand removeBlogCommand;
        private readonly DelegateCommand showFilesCommand;
        private readonly DelegateCommand visitBlogCommand;
        private readonly DelegateCommand enqueueSelectedCommand;
        private readonly DelegateCommand crawlCommand;
        private readonly DelegateCommand pauseCommand;
        private readonly DelegateCommand resumeCommand;
        private readonly DelegateCommand stopCommand;
        private readonly DelegateCommand listenClipboardCommand;
        private readonly DelegateCommand autoDownloadCommand;
        private readonly DelegateCommand showDetailsCommand;
        private readonly List<Task> runningTasks;
        private readonly List<string> deferredDeletions;
        private CancellationTokenSource crawlBlogsCancellation;
        private PauseTokenSource crawlBlogsPause;
        private readonly object lockObject = new object();
        private bool locked = false;

        public delegate void BlogManagerFinishedLoadingHandler(object sender, EventArgs e);
        public event BlogManagerFinishedLoadingHandler BlogManagerFinishedLoading;

        [ImportingConstructor]
        public ManagerController(IShellService shellService, IEnvironmentService environmentService, SelectionService selectionService, CrawlerService crawlerService,
            Lazy<ManagerViewModel> managerViewModel)
        {
            this.shellService = shellService;
            this.environmentService = environmentService;
            this.selectionService = selectionService;
            this.crawlerService = crawlerService;
            this.managerViewModel = managerViewModel;
            this.blogFiles = new ObservableCollection<Blog>();
            this.addBlogCommand = new DelegateCommand(AddBlog, CanAddBlog);
            this.removeBlogCommand = new DelegateCommand(RemoveBlog, CanRemoveBlog);
            this.showFilesCommand = new DelegateCommand(ShowFiles, CanShowFiles);
            this.visitBlogCommand = new DelegateCommand(VisitBlog, CanVisitBlog);
            this.enqueueSelectedCommand = new DelegateCommand(EnqueueSelected, CanEnqueueSelected);
            this.crawlCommand = new DelegateCommand(Crawl, CanCrawl);
            this.pauseCommand = new DelegateCommand(Pause, CanPause);
            this.resumeCommand = new DelegateCommand(Resume, CanResume);
            this.stopCommand = new DelegateCommand(Stop, CanStop);
            this.listenClipboardCommand = new DelegateCommand(ListenClipboard);
            this.autoDownloadCommand = new DelegateCommand(EnqueueAutoDownload, CanEnqueueAutoDownload);
            this.showDetailsCommand = new DelegateCommand(ShowDetailsCommand);
            this.runningTasks = new List<Task>();
            this.deferredDeletions = new List<string>();
            this.locked = false;
        }

        private ManagerViewModel ManagerViewModel { get { return managerViewModel.Value; } }

        public ManagerSettings ManagerSettings { get; set; }

        public QueueManager QueueManager { get; set; }

        public async Task Initialize()
        {
            crawlerService.AddBlogCommand = addBlogCommand;
            crawlerService.RemoveBlogCommand = removeBlogCommand;
            crawlerService.ShowFilesCommand = showFilesCommand;
            crawlerService.EnqueueSelectedCommand = enqueueSelectedCommand;
            crawlerService.CrawlCommand = crawlCommand;
            crawlerService.PauseCommand = pauseCommand;
            crawlerService.ResumeCommand = resumeCommand;
            crawlerService.StopCommand = stopCommand;
            crawlerService.AutoDownloadCommand = autoDownloadCommand;
            crawlerService.ListenClipboardCommand = listenClipboardCommand;
            crawlerService.PropertyChanged += CrawlerServicePropertyChanged;

            ManagerViewModel.ShowFilesCommand = showFilesCommand;
            ManagerViewModel.VisitBlogCommand = visitBlogCommand;
            ManagerViewModel.ShowDetailsCommand = showDetailsCommand;

            ManagerViewModel.PropertyChanged += ManagerViewModelPropertyChanged;

            ManagerViewModel.QueueItems = QueueManager.Items;
            QueueManager.Items.CollectionChanged += QueueItemsCollectionChanged;
            ManagerViewModel.QueueItems.CollectionChanged += ManagerViewModel.QueueItemsCollectionChanged;

            shellService.ContentView = ManagerViewModel.View;

            await LoadLibrary();

            if (shellService.Settings.CheckClipboard)
                shellService.ClipboardMonitor.OnClipboardContentChanged += OnClipboardContentChanged;
        }

        private void QueueItemsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                ManagerViewModel.QueueItems = QueueManager.Items;

            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                ManagerViewModel.QueueItems = QueueManager.Items;
            }
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
                    SaveBlog(blog);
                }
            }
            foreach (string fileToDelete in deferredDeletions)
            {
                File.Delete(fileToDelete);
            }
        }

        private async Task LoadLibrary()
        {
            Logger.Verbose("ManagerController.UpdateBlogFiles:Start");
            selectionService.BlogFiles.Clear();
            var path = Path.Combine(shellService.Settings.DownloadLocation, "Index");

            try
            {
                if (Directory.Exists(path))
                {
                    {
                        var files = await GetFilesAsync(path);
                        foreach (var file in files)
                        {
                            selectionService.BlogFiles.Add(file);
                        }

                        if (BlogManagerFinishedLoading != null)
                        {
                            BlogManagerFinishedLoading(this, EventArgs.Empty);
                        }

                        if (shellService.Settings.CheckOnlineStatusAtStartup == true)
                        {
                            foreach (var file in files)
                            {
                                file.Online = await IsBlogOnline(file.Url);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error("ManagerController:LoadLibrary: {0}", ex);
                shellService.ShowError(ex, Resources.CouldNotLoadLibrary, ex.Data["Filename"]);
                return;
            }
            Logger.Verbose("ManagerController.LoadLibrary:End");
        }

        private Task<IReadOnlyList<Blog>> GetFilesAsync(string directory)
        {
            // run this in an own task:
            return Task<IReadOnlyList<Blog>>.Factory.StartNew(() =>
            {
                return GetFilesCore(directory);
            },
            TaskCreationOptions.LongRunning);
        }

        private IReadOnlyList<Blog> GetFilesCore(string directory)
        {
            Logger.Verbose("ManagerController.UpdateBlogFiles:GetFilesAsync Start");

            List<Blog> blogs = new List<Blog>();

            foreach (var filename in Directory.GetFiles(directory, "*.tumblr"))
            {
                using (FileStream stream = new FileStream(filename,
                    FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    IFormatter formatter = new BinaryFormatter();
                    try
                    {
                        TumblrBlog blog = (TumblrBlog)formatter.Deserialize(stream);
                        blogs.Add(blog);
                    }
                    catch (SerializationException ex)
                    {
                        ex.Data["Filename"] = filename;
                        throw;
                    }
                }
            }
            Logger.Verbose("ManagerController.UpdateBlogFiles:GetFilesAsync End");

            return blogs;
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
            removeBlogCommand.RaiseCanExecuteChanged();


            for (int i = 0; i < shellService.Settings.ParallelBlogs; i++)
                runningTasks.Add(
                Task.Factory.StartNew(() => runCrawlerTasks(cancellation.Token, pause.Token),
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

        private void runCrawlerTasks(CancellationToken ct, PauseToken pt)
        {
            while (true)
            {
                // check if stopped
                if (ct.IsCancellationRequested)
                {
                    //break;
                    throw new OperationCanceledException(ct);
                }

                // check if paused
                if (pt.IsPaused)
                    pt.WaitWhilePausedWithResponseAsyc().Wait();

                Monitor.Enter(QueueManager.Items);
                if (selectionService.ActiveItems.Count() < QueueManager.Items.Count())
                {

                    var blogListToCrawlNext = QueueManager.Items.Except(selectionService.ActiveItems);
                    var blogToCrawlNext = blogListToCrawlNext.First();

                    blogToCrawlNext.Blog.Online = IsBlogOnline(blogToCrawlNext.Blog.Url).Result;

                    if (selectionService.ActiveItems.Any(item => item.Blog.Name.Contains(blogToCrawlNext.Blog.Name)))
                    {
                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() => {
                                Monitor.Enter(QueueManager.Items);
                                QueueManager.RemoveItem(blogToCrawlNext);
                                Monitor.Exit(QueueManager.Items);
                            }));
                        Monitor.Exit(QueueManager.Items);
                        continue;
                    }

                    if (!blogToCrawlNext.Blog.Online)
                    {
                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new Action(() =>
                            {
                                Monitor.Enter(QueueManager.Items);
                                QueueManager.RemoveItem(blogToCrawlNext);
                                Monitor.Exit(QueueManager.Items);
                            }));
                        Monitor.Exit(QueueManager.Items);
                        continue;
                    }
                        
                    Monitor.Enter(selectionService.ActiveItems);
                    selectionService.AddActiveItems(blogToCrawlNext);
                    Monitor.Exit(selectionService.ActiveItems);

                    Monitor.Exit(QueueManager.Items);

                    if (blogToCrawlNext.Blog is TumblrBlog)
                    {

                        var blog = (TumblrBlog)blogToCrawlNext.Blog;
                        blog.Dirty = true;

                        var progressHandler = new Progress<DataModels.DownloadProgress>(value =>
                        {
                            blogToCrawlNext.Progress = value.Progress;
                        });
                        var progress = progressHandler as IProgress<DataModels.DownloadProgress>;

                        CrawlCoreTumblrBlog(blog, progress, ct, pt);

                        if (ct.IsCancellationRequested)
                        {
                            Application.Current.Dispatcher.BeginInvoke(
                                DispatcherPriority.Background,
                                new Action(() => {
                                    Monitor.Enter(selectionService.ActiveItems);
                                    selectionService.RemoveActiveItem(blogToCrawlNext);
                                    Monitor.Exit(selectionService.ActiveItems);
                                }));
                            throw new OperationCanceledException(ct);
                        }
                        else
                        {
                            Application.Current.Dispatcher.BeginInvoke(
                                DispatcherPriority.Background,
                                new Action(() => {
                                    Monitor.Enter(QueueManager.Items);
                                    QueueManager.RemoveItem(blogToCrawlNext);
                                    Monitor.Exit(QueueManager.Items);

                                    Monitor.Enter(selectionService.ActiveItems);
                                    selectionService.RemoveActiveItem(blogToCrawlNext);
                                    Monitor.Exit(selectionService.ActiveItems);
                                }));
                        }
                    }
                }
                else
                {
                    Monitor.Exit(QueueManager.Items);
                    Task.Delay(4000, ct).Wait();
                }
            }

        }

        private TumblrBlog CrawlCoreTumblrBlog(TumblrBlog blog, IProgress<DataModels.DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            Logger.Verbose("ManagerController.CrawlCoreTumblrBlog:Start");

            var tuple = GetImageUrls(blog, progress, ct, pt);
            var newImageCount = tuple.Item1;
            var newImageUrls = tuple.Item2;

            int downloadedImages = (int)blog.DownloadedImages;
            int downloadedPhotos = (int)blog.DownloadedPhotos;
            int downloadedVideos = (int)blog.DownloadedVideos;
            int downloadedAudios = (int)blog.DownloadedAudios;
            int downloadedTexts = (int)blog.DownloadedTexts;
            int downloadedQuotes = (int)blog.DownloadedQuotes;
            int downloadedLinks = (int)blog.DownloadedLinks;
            int downloadedConversations = (int)blog.DownloadedConversations;
            int downloadedPhotoMetas = (int)blog.DownloadedPhotoMetas;
            int downloadedVideoMetas = (int)blog.DownloadedVideoMetas;
            int downloadedAudioMetas = (int)blog.DownloadedAudioMetas;

            object lockObjectProgress = new object();
            object lockObjectDownload = new object();
            bool locked = false;

            blog.TotalCount = newImageCount;

            var newProgress = new DataModels.DownloadProgress();
            newProgress.Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressUniqueDownloads);
            progress.Report(newProgress);

            // determine duplicates
            int duplicatePhotos = newImageUrls.Where(url => url.Item2.Equals("Photo"))
                .GroupBy(url => url.Item1)
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count() - 1);
            int duplicateVideos = newImageUrls.Where(url => url.Item2.Equals("Video"))
                .GroupBy(url => url.Item1)
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count() - 1);
            int duplicateAudios = newImageUrls.Where(url => url.Item2.Equals("Audio"))
                .GroupBy(url => url.Item1)
                .Where(g => g.Count() > 1)
                .Sum(g => g.Count() - 1);

            int duplicates = duplicatePhotos + duplicateVideos + duplicateAudios;

            blog.DuplicatePhotos = (uint)duplicatePhotos;
            blog.DuplicateVideos = (uint)duplicateVideos;
            blog.DuplicateAudios = (uint)duplicateAudios;

            // remove the duplicates from the download list
            var imageUrls = new HashSet<Tuple<string, string, string>>(newImageUrls);

            // remove all files previously downloaded
            var blogLinks = new HashSet<string>(blog.Links.Select(item => item?.Split('/').Last()));
            imageUrls.RemoveWhere(item => blogLinks.Contains(item.Item1.Split('/').Last()));
            imageUrls.RemoveWhere(item => blogLinks.Contains(item.Item3));

            var indexPath = Path.Combine(shellService.Settings.DownloadLocation, "Index");
            var blogPath = shellService.Settings.DownloadLocation;

            // make sure the datafolder still exists
            CreateDataFolder(blog.Name, blogPath);

            var parallel = Parallel.ForEach(
                imageUrls,
                    new ParallelOptions { MaxDegreeOfParallelism = (shellService.Settings.ParallelImages / selectionService.ActiveItems.Count) },
                    (currentImageUrl, state) =>
                    {
                        if (ct.IsCancellationRequested)
                        {
                            state.Break();
                        }
                        if (pt.IsPaused)
                            pt.WaitWhilePausedWithResponseAsyc().Wait();

                        string fileName = String.Empty;
                        string fileLocation = String.Empty;

                        switch (currentImageUrl.Item2)
                        {
                            case "Photo":
                                fileName = currentImageUrl.Item1.Split('/').Last();
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), fileName);

                                if (Download(blog, fileLocation, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedPhotos, ref downloadedImages))
                                {
                                    lock (lockObjectProgress)
                                    {
                                        blog.Links.Add(currentImageUrl.Item1);
                                        // could be moved out of the lock?
                                        blog.DownloadedImages = (uint)downloadedImages;
                                        blog.Progress = (uint)(((double)downloadedImages + (double)duplicates) / (double)blog.TotalCount * 100);
                                        blog.DownloadedPhotos = (uint)downloadedPhotos;
                                    }
                                    if (shellService.Settings.EnablePreview)
                                        blog.LastDownloadedPhoto = Path.GetFullPath(fileLocation);
                                }
                                break;
                            case "Video":
                                fileName = currentImageUrl.Item1.Split('/').Last();
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), fileName);

                                if (Download(blog, fileLocation, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedVideos, ref downloadedImages))
                                {
                                    lock (lockObjectProgress)
                                    {
                                        blog.Links.Add(currentImageUrl.Item1);
                                        // could be moved out of the lock?
                                        blog.DownloadedImages = (uint)downloadedImages;
                                        blog.Progress = (uint)(((double)downloadedImages + (double)duplicates) / (double)blog.TotalCount * 100);
                                        blog.DownloadedVideos = (uint)downloadedVideos;
                                    }
                                    if (shellService.Settings.EnablePreview)
                                        blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);
                                }
                                break;
                            case "Audio":
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), currentImageUrl.Item3 + ".swf");

                                if (Download(blog, fileLocation, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedAudios, ref downloadedImages))
                                {
                                    lock (lockObjectProgress)
                                    {
                                        blog.Links.Add(currentImageUrl.Item1);
                                        // could be moved out of the lock?
                                        blog.DownloadedImages = (uint)downloadedImages;
                                        blog.Progress = (uint)(((double)downloadedImages + (double)duplicates) / (double)blog.TotalCount * 100);
                                        blog.DownloadedAudios = (uint)downloadedAudios;
                                    }
                                }
                                break;
                            case "Text":
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameTexts));

                                if (Download(blog, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedTexts, ref downloadedImages))
                                {
                                    lock (lockObjectProgress)
                                    {
                                        blog.Links.Add(currentImageUrl.Item3);
                                        // could be moved out of the lock?
                                        blog.DownloadedImages = (uint)downloadedImages;
                                        blog.Progress = (uint)(((double)downloadedImages + (double)duplicates) / (double)blog.TotalCount * 100);
                                        blog.DownloadedTexts = (uint)downloadedTexts;
                                    }
                                }
                                break;
                            case "Quote":
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameQuotes));

                                if (Download(blog, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedQuotes, ref downloadedImages))
                                {
                                    lock (lockObjectProgress)
                                    {
                                        blog.Links.Add(currentImageUrl.Item3);
                                        // could be moved out of the lock?
                                        blog.DownloadedImages = (uint)downloadedImages;
                                        blog.Progress = (uint)(((double)downloadedImages + (double)duplicates) / (double)blog.TotalCount * 100);
                                        blog.DownloadedQuotes = (uint)downloadedQuotes;
                                    }
                                }
                                break;
                            case "Link":
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameLinks));

                                if (Download(blog, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedLinks, ref downloadedImages))
                                {
                                    lock (lockObjectProgress)
                                    {
                                        blog.Links.Add(currentImageUrl.Item3);
                                        // could be moved out of the lock?
                                        blog.DownloadedImages = (uint)downloadedImages;
                                        blog.Progress = (uint)(((double)downloadedImages + (double)duplicates) / (double)blog.TotalCount * 100);
                                        blog.DownloadedLinks = (uint)downloadedLinks;
                                    }
                                }
                                break;
                            case "Conversation":
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameConversations));

                                if (Download(blog, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedConversations, ref downloadedImages))
                                {
                                    lock (lockObjectProgress)
                                    {
                                        blog.Links.Add(currentImageUrl.Item3);
                                        // could be moved out of the lock?
                                        blog.DownloadedImages = (uint)downloadedImages;
                                        blog.Progress = (uint)(((double)downloadedImages + (double)duplicates) / (double)blog.TotalCount * 100);
                                        blog.DownloadedConversations = (uint)downloadedConversations;
                                    }
                                }
                                break;
                            case "PhotoMeta":
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaPhoto));

                                if (Download(blog, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedPhotoMetas, ref downloadedImages))
                                {
                                    lock (lockObjectProgress)
                                    {
                                        blog.Links.Add(currentImageUrl.Item3);
                                        // could be moved out of the lock?
                                        blog.DownloadedImages = (uint)downloadedImages;
                                        blog.Progress = (uint)(((double)downloadedImages + (double)duplicates) / (double)blog.TotalCount * 100);
                                        blog.DownloadedPhotoMetas = (uint)downloadedPhotoMetas;
                                    }
                                }
                                break;
                            case "VideoMeta":
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaVideo));

                                if (Download(blog, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedVideoMetas, ref downloadedImages))
                                {
                                    lock (lockObjectProgress)
                                    {
                                        blog.Links.Add(currentImageUrl.Item3);
                                        // could be moved out of the lock?
                                        blog.DownloadedImages = (uint)downloadedImages;
                                        blog.Progress = (uint)(((double)downloadedImages + (double)duplicates) / (double)blog.TotalCount * 100);
                                        blog.DownloadedVideoMetas = (uint)downloadedVideoMetas;
                                    }
                                }
                                break;
                            case "AudioMeta":
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaAudio));

                                if (Download(blog, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedAudioMetas, ref downloadedImages))
                                {
                                    lock (lockObjectProgress)
                                    {
                                        blog.Links.Add(currentImageUrl.Item3);
                                        // could be moved out of the lock?
                                        blog.DownloadedImages = (uint)downloadedImages;
                                        blog.Progress = (uint)(((double)downloadedImages + (double)duplicates) / (double)blog.TotalCount * 100);
                                        blog.DownloadedAudioMetas = (uint)downloadedAudioMetas;
                                    }
                                }
                                break;
                            default:
                                break;
                        }
                    });

            if (!ct.IsCancellationRequested)
            {
                blog.LastCompleteCrawl = DateTime.Now;
            }
            blog.LastDownloadedPhoto = null;
            blog.LastDownloadedVideo = null;
            blog.Dirty = false;
            SaveBlog(blog);

            newProgress = new DataModels.DownloadProgress();
            newProgress.Progress = "";
            progress.Report(newProgress);

            return blog;
        }

        private bool CanEnqueueSelected()
        {
            return ManagerViewModel.SelectedBlogFile != null && ManagerViewModel.SelectedBlogFile.Online;
        }

        private void EnqueueSelected()
        {
            Enqueue(selectionService.SelectedBlogFiles.Where(blog => blog.Online).ToArray());
        }

        private void Enqueue(IEnumerable<IBlog> blogFiles)
        {
            QueueManager.AddItems(blogFiles.Select(x => new QueueListItem(x)));
            //shellService.ShowQueueView();
        }

        private bool CanEnqueueAutoDownload()
        {
            return selectionService.BlogFiles.Any();
        }

        private void EnqueueAutoDownload()
        {
            if (shellService.Settings.BlogType == shellService.Settings.BlogTypes.ElementAtOrDefault(0))
            {
            }
            if (shellService.Settings.BlogType == shellService.Settings.BlogTypes.ElementAtOrDefault(1))
                Enqueue(selectionService.BlogFiles.Where(blog => blog.Online).ToArray());
            if (shellService.Settings.BlogType == shellService.Settings.BlogTypes.ElementAtOrDefault(2))
                Enqueue(selectionService.BlogFiles.Where(blog => blog.Online && blog.LastCompleteCrawl != System.DateTime.MinValue).ToArray());
            if (shellService.Settings.BlogType == shellService.Settings.BlogTypes.ElementAtOrDefault(3))
                Enqueue(selectionService.BlogFiles.Where(blog => blog.Online && blog.LastCompleteCrawl == System.DateTime.MinValue).ToArray());

            if (crawlerService.IsCrawl && crawlerService.IsPaused)
            {
                resumeCommand.CanExecute(null);
                resumeCommand.Execute(null);
            }
            else if (!crawlerService.IsCrawl)
            {
                crawlCommand.CanExecute(null);
                crawlCommand.Execute(null);
            }
        }

        private bool CanAddBlog() { return Validator.IsValidTumblrUrl(crawlerService.NewBlogUrl); }

        public void AddBlog()
        {
            Task.Factory.StartNew(() =>
            {
                return AddBlogAsync(null);
            },
            TaskCreationOptions.LongRunning);
        }


        private bool CanRemoveBlog()
        {
            return ManagerViewModel.SelectedBlogFile != null;
        }

        public void RemoveBlog()
        {
            var indexPath = Path.Combine(shellService.Settings.DownloadLocation, "Index");
            var blogPath = shellService.Settings.DownloadLocation;

            var blogs = selectionService.SelectedBlogFiles.ToArray();

            foreach (var blog in blogs)
            {
                if (!shellService.Settings.DeleteOnlyIndex)
                {
                    try
                    {
                        Directory.Delete(Path.Combine(blogPath, blog.Name), true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("ManagerController:RemoveBlog: {0}", ex);
                        shellService.ShowError(ex, Resources.CouldNotRemoveBlog, blog.Name);
                        return;
                    }
                }

                var indexFile = Path.Combine(indexPath, blog.Name) + ".tumblr";
                try
                {
                    File.Delete(indexFile);
                }
                catch (Exception ex)
                {
                    Logger.Error("ManagerController:RemoveBlog: {0}", ex);
                    shellService.ShowError(ex, Resources.CouldNotRemoveBlogIndex, blog.Name);
                    return;
                }

                selectionService.BlogFiles.Remove(blog);
                QueueManager.RemoveItems(QueueManager.Items.Where(item => item.Blog.Equals(blog)));
            }
        }

        private bool CanShowFiles()
        {
            return ManagerViewModel.SelectedBlogFile != null;
        }

        public void ShowFiles()
        {
            var path = shellService.Settings.DownloadLocation;

            foreach (var blog in selectionService.SelectedBlogFiles.ToArray())
            {
                System.Diagnostics.Process.Start("explorer.exe", Path.Combine(path, blog.Name));

            }
        }

        private bool CanVisitBlog()
        {
            return ManagerViewModel.SelectedBlogFile != null;
        }

        public void VisitBlog()
        {
            foreach (var blog in selectionService.SelectedBlogFiles.ToArray())
            {
                System.Diagnostics.Process.Start(blog.Url);
            }
        }

        private void ShowDetailsCommand()
        {
            shellService.ShowDetailsView();
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

        private Task<bool> IsBlogOnline(string url)
        {
            return Task<bool>.Factory.StartNew(() =>
            {
                url = GetApiUrl(url, 1);

                var blogDoc = RequestData(url);
                if (blogDoc != null)
                    return true;
                else
                    return false;
            },
            TaskCreationOptions.LongRunning);
        }

        private string GetApiUrl(string url, int count, int start = 0)
        {
            /// <summary>
            /// construct the tumblr api post url of a blog.
            /// <para>the blog for the url</para>
            /// </summary>
            /// <returns>A string containing the api url of the blog.</returns>
            if (url.Last<char>() != '/')
                url += "/api/read";
            else
                url += "api/read";

            var parameters = new Dictionary<string, string>
            {
              { "num", count.ToString() }
            };
            if (start > 0)
                parameters["start"] = start.ToString();
            return url + "?" + UrlEncode(parameters);
        }

        private XDocument RequestData(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                if (!String.IsNullOrEmpty(shellService.Settings.ProxyHost))
                {
                    request.Proxy = new WebProxy(shellService.Settings.ProxyHost, Int32.Parse(shellService.Settings.ProxyPort));
                } else
                {
                    request.Proxy = null; // WebRequest.GetSystemWebProxy();
                }
                request.KeepAlive = true;
                request.AllowAutoRedirect = true;
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.Pipelined = true;
                request.Timeout = shellService.Settings.TimeOut * 1000;
                request.ServicePoint.Expect100Continue = false;
                ServicePointManager.DefaultConnectionLimit = 400;
                //request.ContentLength = 0;
                //request.ContentType = "x-www-from-urlencoded";

                // should we throttle?
                int bandwidth = 2000000;
                if (shellService.Settings.LimitScanBandwidth)
                    bandwidth = shellService.Settings.Bandwidth;

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (ThrottledStream stream = new ThrottledStream(response.GetResponseStream(), (bandwidth / shellService.Settings.ParallelImages) * 1024))
                    {
                        using (BufferedStream buffer = new BufferedStream(stream))
                        {
                            using (StreamReader reader = new StreamReader(buffer))
                            {
                                //Doesn't work because the tumblr XML api delivers malformated XML. Nice!
                                //XmlSerializer xmlSerializer = new XmlSerializer(typeof(Datamodels.Xml.Tumblr));
                                //Datamodels.Xml.Tumblr data = (Datamodels.Xml.Tumblr)xmlSerializer.Deserialize(reader);
                                //return data;
                                return XDocument.Load(reader);
                            }
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                return null;
            }
        }

        private static string UrlEncode(IDictionary<string, string> parameters)
        {
            var sb = new StringBuilder();
            foreach (var val in parameters)
            {
                // add each parameter to the query string, url-encoding the value.
                sb.AppendFormat("{0}={1}&", val.Key, HttpUtility.UrlEncode(val.Value));
            }
            sb.Remove(sb.Length - 1, 1); // remove last '&'
            return sb.ToString();
        }

        private string ExtractUrl(string url)
        {
            return ("https://" + ExtractBlogname(url) + ".tumblr.com/");
        }

        public async Task AddBlogAsync(string blogUrl)
        {
            if (String.IsNullOrEmpty(blogUrl))
            {
                blogUrl = crawlerService.NewBlogUrl;
            }

            string blogName = ExtractBlogname(blogUrl);
            TumblrBlog blog = new TumblrBlog(ExtractUrl(blogUrl));

            blog.Name = blogName;
            blog.DownloadAudio = shellService.Settings.DownloadAudios;
            blog.DownloadPhoto = shellService.Settings.DownloadImages;
            blog.DownloadVideo = shellService.Settings.DownloadVideos;
            blog.DownloadText = shellService.Settings.DownloadTexts;
            blog.DownloadQuote = shellService.Settings.DownloadQuotes;
            blog.DownloadConversation = shellService.Settings.DownloadConversations;
            blog.DownloadLink = shellService.Settings.DownloadLinks;
            blog.CreatePhotoMeta = shellService.Settings.CreateImageMeta;
            blog.CreateVideoMeta = shellService.Settings.CreateVideoMeta;
            blog.CreateAudioMeta = shellService.Settings.CreateAudioMeta;
            blog.SkipGif = shellService.Settings.SkipGif;
            blog.ForceSize = shellService.Settings.ForceSize;
            
            blog = await GetMetaInformation(blog);
            blog.Online = await IsBlogOnline(blog.Url);

            if (SaveBlog(blog))
            {
                lock (lockObject)
                {
                    if (selectionService.BlogFiles.Select(blogs => blogs.Name).ToList().Contains(blogName))
                    {
                        shellService.ShowError(null, Resources.BlogAlreadyExist, blogName);
                        return;
                    }


                    if (Application.Current.Dispatcher.CheckAccess())
                    {
                        selectionService.BlogFiles.Add(blog);
                    }
                    else
                    {
                        Application.Current.Dispatcher.BeginInvoke(
                          DispatcherPriority.Background,
                          new Action(() =>
                          {
                              selectionService.BlogFiles.Add(blog);
                          }));
                    }
                }
            }
        }

        public bool SaveBlog(Blog blog)
        {
            if (blog == null)
                return false;

            var indexPath = Path.Combine(shellService.Settings.DownloadLocation, "Index");
            var blogPath = shellService.Settings.DownloadLocation;

            string currentIndex = Path.Combine(indexPath, blog.Name + ".tumblr");
            string newIndex = Path.Combine(indexPath, blog.Name + ".tumblr.new");
            string backupIndex = Path.Combine(indexPath, blog.Name + ".tumblr.bak");

            try
            {
                CreateDataFolder("Index", blogPath);
                CreateDataFolder(blog.Name, blogPath);

                if (File.Exists(currentIndex))
                {
                    using (FileStream stream = new FileStream(newIndex, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        IFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, blog);
                    }

                    File.Replace(newIndex, currentIndex, backupIndex, true);
                    try
                    {
                        File.Delete(backupIndex);
                    }
                    catch
                    {
                        deferredDeletions.Add(backupIndex);
                    }
                }
                else
                {
                    using (FileStream stream = new FileStream(currentIndex, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        IFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, blog);
                    }
                }
            
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("ManagerController:SaveBlog: {0}", ex);
                shellService.ShowError(ex, Resources.CouldNotSaveBlog, blog.Name);
                return false;
            }
        }


        public bool CreateDataFolder(string name, string location)
        {
            if (String.IsNullOrEmpty(name))
                return false;

            if (!Directory.Exists(Path.Combine(location, name)))
            {
                Directory.CreateDirectory(Path.Combine(location, name));
                return true;
            }
            return true;
        }

        private Task<TumblrBlog> GetMetaInformation(TumblrBlog blog)
        {
            return Task<TumblrBlog>.Factory.StartNew(() =>
            {
                string url = GetApiUrl(blog.Url, 1);

                var blogDoc = RequestData(url);

                if (blogDoc != null)
                {
                    blog.Title = blogDoc.Element("tumblr").Element("tumblelog").Attribute("title")?.Value;
                    blog.Description = blogDoc.Element("tumblr").Element("tumblelog")?.Value;
                    blog.TotalCount = UInt32.Parse(blogDoc.Element("tumblr").Element("posts").Attribute("total")?.Value);
                    return blog;
                }
                else
                    return blog;
            },
            TaskCreationOptions.LongRunning);
        }

        public Tuple<uint, List<Tuple<string, string, string>>> GetImageUrls(TumblrBlog blog, IProgress<Datamodels.DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            int totalPosts = 0;
            int numberOfPostsCrawled = 0;
            int totalDownloads = 0;
            int photos = 0;
            int videos = 0;
            int audios = 0;
            int texts = 0;
            int conversations = 0;
            int quotes = 0;
            int links = 0;
            int photoMetas = 0;
            int videoMetas = 0;
            int audioMetas = 0;
            List<Tuple<string, string, string>> images = new List<Tuple<string, string, string>>();

            string url = GetApiUrl(blog.Url, 1);

            var blogDoc = RequestData(url);
            totalPosts = Int32.Parse(blogDoc.Element("tumblr").Element("posts").Attribute("total").Value);

            // Generate URL list of Images
            // the api shows 50 posts at max, determine the number of pages to crawl
            int totalPages = (totalPosts / 50) + 1;

            // FIXME: We should use more parallel downloads as requested here since the data is usually really small
            Parallel.For(0, totalPages,
                        new ParallelOptions { MaxDegreeOfParallelism = (shellService.Settings.ParallelScans) },
                        (i, state) =>
                        {
                            if (ct.IsCancellationRequested)
                            {
                                state.Break();
                            }
                            if (pt.IsPaused)
                                pt.WaitWhilePausedWithResponseAsyc().Wait();
                            try
                            {
                                // check for tags -- crawling for all images here
                                if (blog.Tags == null || blog.Tags.Count() == 0)
                                {
                                    XDocument document = null;

                                    // get 50 posts per crawl/page
                                    url = GetApiUrl(blog.Url, 50, i * 50);
                                    document = RequestData(url);

                                    //FIXME: Create Generic Method to reduce WET code
                                    // Doesn't count photosets
                                    //Interlocked.Add(ref photos, document.Descendants("post").Where(post => post.Attribute("type").Value == "photo").Count());
                                    Interlocked.Add(ref videos, document.Descendants("post").Where(post => post.Attribute("type").Value == "video").Count());
                                    Interlocked.Add(ref audios, document.Descendants("post").Where(post => post.Attribute("type").Value == "audio").Count());
                                    Interlocked.Add(ref texts, document.Descendants("post").Where(post => post.Attribute("type").Value == "regular").Count());
                                    Interlocked.Add(ref conversations, document.Descendants("post").Where(post => post.Attribute("type").Value == "conversation").Count());
                                    Interlocked.Add(ref quotes, document.Descendants("post").Where(post => post.Attribute("type").Value == "quote").Count());
                                    Interlocked.Add(ref links, document.Descendants("post").Where(post => post.Attribute("type").Value == "link").Count());

                                    if (blog.DownloadPhoto == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "photo"))
                                        {
                                            // photoset
                                            if (post.Descendants("photoset").Count() > 0)
                                            {
                                                foreach (var photo in post.Descendants("photoset").Descendants("photo"))
                                                {
                                                    var imageUrl = photo.Elements("photo-url").Where(photo_url =>
                                                        photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).FirstOrDefault().Value;

                                                    if (blog.SkipGif == true && imageUrl.EndsWith(".gif"))
                                                        continue;

                                                    if (blog.ForceSize)
                                                    {
                                                        StringBuilder sb = new StringBuilder(imageUrl);
                                                        imageUrl = sb
                                                          .Replace("_1280", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_540", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_500", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_400", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_250", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_100", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_75sq", "_" + shellService.Settings.ImageSize.ToString())
                                                          .ToString();
                                                    }

                                                    Interlocked.Increment(ref photos);
                                                    Interlocked.Increment(ref totalDownloads);
                                                    Monitor.Enter(images);
                                                    images.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                                                    Monitor.Exit(images);
                                                }
                                            }
                                            // single image
                                            else
                                            {
                                                var imageUrl = post.Elements("photo-url").Where(photo_url =>
                                                        photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).FirstOrDefault().Value;

                                                if (blog.SkipGif == true && imageUrl.EndsWith(".gif"))
                                                    continue;

                                                if (blog.ForceSize)
                                                {
                                                    StringBuilder sb = new StringBuilder(imageUrl);
                                                    imageUrl = sb
                                                      .Replace("_1280", "_" + shellService.Settings.ImageSize.ToString())
                                                      .Replace("_540", "_" + shellService.Settings.ImageSize.ToString())
                                                      .Replace("_500", "_" + shellService.Settings.ImageSize.ToString())
                                                      .Replace("_400", "_" + shellService.Settings.ImageSize.ToString())
                                                      .Replace("_250", "_" + shellService.Settings.ImageSize.ToString())
                                                      .Replace("_100", "_" + shellService.Settings.ImageSize.ToString())
                                                      .Replace("_75sq", "_" + shellService.Settings.ImageSize.ToString())
                                                      .ToString();
                                                }

                                                Interlocked.Increment(ref photos);
                                                Interlocked.Increment(ref totalDownloads);
                                                Monitor.Enter(images);
                                                images.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                                                Monitor.Exit(images);
                                            }
                                        }
                                        // check for inline images
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "regular"))
                                        {
                                            if (post.Element("regular-body").Value.Contains("tumblr_inline"))
                                            {
                                                Regex regex = new Regex("<img src=\"(.*?)\"");
                                                foreach (Match match in regex.Matches(post.Element("regular-body")?.Value))
                                                {
                                                    var imageUrl = match.Groups[1].Value;
                                                    if (blog.ForceSize)
                                                    {
                                                        StringBuilder sb = new StringBuilder(imageUrl);
                                                        imageUrl = sb
                                                          .Replace("_1280", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_540", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_500", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_400", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_250", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_100", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_75sq", "_" + shellService.Settings.ImageSize.ToString())
                                                          .ToString();
                                                    }

                                                    Interlocked.Increment(ref totalDownloads);
                                                    Monitor.Enter(images);
                                                    images.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                                                    Monitor.Exit(images);
                                                }
                                            }
                                        }
                                    }
                                    if (blog.DownloadVideo == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "video"))
                                        {
                                            var videoUrl = post.Descendants("video-player").Where(x => x.Value.Contains("<source src=")).Select(result =>
                                                System.Text.RegularExpressions.Regex.Match(
                                                result.Value, "<source src=\"(.*)\" type=\"video/mp4\">").Groups[1].Value).ToList();

                                            foreach (string video in videoUrl)
                                            {
                                                if (shellService.Settings.VideoSize == 1080)
                                                {
                                                    Interlocked.Increment(ref totalDownloads);
                                                    Monitor.Enter(images);
                                                    images.Add(Tuple.Create(video.Replace("/480", "") + ".mp4", "Video", post.Attribute("id").Value));
                                                    Monitor.Exit(images);
                                                }
                                                else if (shellService.Settings.VideoSize == 480)
                                                {
                                                    Interlocked.Increment(ref totalDownloads);
                                                    Monitor.Enter(images);
                                                    images.Add(Tuple.Create("https://vt.tumblr.com/" + video.Replace("/480", "").Split('/').Last() + "_480.mp4", "Video", post.Attribute("id").Value));
                                                    Monitor.Exit(images);
                                                }
                                            }
                                        }
                                    }
                                    if (blog.DownloadAudio == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "audio"))
                                        {
                                            var audioUrl = post.Descendants("audio-player").Where(x => x.Value.Contains("src=")).Select(result =>
                                                System.Text.RegularExpressions.Regex.Match(
                                                result.Value, "src=\"(.*)\" height").Groups[1].Value).ToList();

                                            foreach (string audiofile in audioUrl)
                                            {
                                                Interlocked.Increment(ref totalDownloads);
                                                Monitor.Enter(images);
                                                images.Add(Tuple.Create(WebUtility.UrlDecode(audiofile), "Audio", post.Attribute("id").Value));
                                                Monitor.Exit(images);
                                            }

                                        }
                                    }
                                    if (blog.DownloadText == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "regular"))
                                        {
                                            string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Title, post.Element("regular-title")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Body, post.Element("regular-body")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                                                Environment.NewLine;
                                            Interlocked.Increment(ref totalDownloads);
                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Text", post.Attribute("id").Value));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.DownloadQuote == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "quote"))
                                        {
                                            string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.Element("quote-text")?.Value) + 
                                                Environment.NewLine + post.Element("quote-source")?.Value +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                                                Environment.NewLine;
                                            Interlocked.Increment(ref totalDownloads);
                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Quote", post.Attribute("id").Value));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.DownloadLink == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "link"))
                                        {
                                            string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Link, post.Element("link-text")?.Value) + 
                                                Environment.NewLine + post.Element("link-url")?.Value +
                                                Environment.NewLine + post.Element("link-description")?.Value +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                                                Environment.NewLine;
                                            Interlocked.Increment(ref totalDownloads);
                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Link", post.Attribute("id").Value));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.DownloadConversation == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "conversation"))
                                        {
                                            string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Conversation, post.Element("conversation-text")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                                                Environment.NewLine;
                                            Interlocked.Increment(ref totalDownloads);
                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Conversation", post.Attribute("id").Value));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.CreatePhotoMeta == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "photo"))
                                        {
                                            string imageUrl = "";
                                            // photoset
                                            if (post.Descendants("photoset").Count() > 0)
                                            {
                                                List<string> imageUrls = new List<string>();
                                                foreach (var photo in post.Descendants("photoset").Descendants("photo"))
                                                {
                                                    var singleImageUrl = photo.Elements("photo-url").Where(photo_url =>
                                                        photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).FirstOrDefault().Value;

                                                    if (blog.SkipGif == true && imageUrl.EndsWith(".gif"))
                                                        continue;
                                                    imageUrls.Add(singleImageUrl);
                                                }
                                                // imageUrl = imageUrls.Aggregate((current, next) => current + ", " + next);
                                                imageUrl = string.Join(", ", imageUrls.ToArray());
                                            }
                                            // single image
                                            {
                                                imageUrl = post.Elements("photo-url").Where(photo_url =>
                                                        photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).FirstOrDefault().Value;

                                                if (blog.SkipGif == true && imageUrl.EndsWith(".gif"))
                                                    continue;

                                                string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                                                    Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                                                    Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                                                    Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.PhotoUrl, imageUrl) +
                                                    Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.PhotoCaption, post.Element("photo-caption")?.Value) +
                                                    Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                                                    Environment.NewLine;
                                                Interlocked.Increment(ref totalDownloads);
                                                Interlocked.Increment(ref photoMetas);
                                                Monitor.Enter(images);
                                                images.Add(Tuple.Create(textBody, "PhotoMeta", post.Attribute("id").Value));
                                                Monitor.Exit(images);
                                            }
                                        }
                                    }
                                    if (blog.CreateVideoMeta == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "video"))
                                        {
                                            string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.VideoPlayer, post.Element("video-player")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                                                Environment.NewLine;
                                            Interlocked.Increment(ref totalDownloads);
                                            Interlocked.Increment(ref videoMetas);
                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "VideoMeta", post.Attribute("id").Value));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.CreateAudioMeta == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "audio"))
                                        {
                                            string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.AudioCaption, post.Element("audio-caption")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Artist, post.Element("id3-artist")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Title, post.Element("id3-title")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.Element("id3-track")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Album, post.Element("id3-album")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Year, post.Element("id3-year")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                                                Environment.NewLine;
                                            Interlocked.Increment(ref totalDownloads);
                                            Interlocked.Increment(ref audioMetas);
                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "AudioMeta", post.Attribute("id").Value));
                                            Monitor.Exit(images);
                                        }
                                    }
                                }
                                // crawling only for tagged images
                                else
                                {
                                    List<string> tags = blog.Tags.Split(',').Select(x => x.Trim()).ToList();

                                    XDocument document = null;

                                    // get 50 posts per crawl/page

                                    url = GetApiUrl(blog.Url, 50, i * 50);
                                    document = RequestData(url);

                                    if (blog.DownloadPhoto == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "photo" && posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                                        {
                                            // photoset
                                            if (post.Descendants("photoset").Count() > 0)
                                            {
                                                foreach (var photo in post.Descendants("photoset").Descendants("photo"))
                                                {
                                                    var imageUrl = photo.Elements("photo-url").Where(photo_url =>
                                                        photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).FirstOrDefault().Value;

                                                    if (blog.SkipGif == true && imageUrl.EndsWith(".gif"))
                                                        continue;

                                                    if (blog.ForceSize)
                                                    {
                                                        StringBuilder sb = new StringBuilder(imageUrl);
                                                        imageUrl = sb
                                                          .Replace("_1280", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_540", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_500", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_400", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_250", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_100", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_75sq", "_" + shellService.Settings.ImageSize.ToString())
                                                          .ToString();
                                                    }

                                                    Interlocked.Increment(ref totalDownloads);
                                                    Interlocked.Increment(ref photos);
                                                    Monitor.Enter(images);
                                                    images.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                                                    Monitor.Exit(images);
                                                }
                                            }
                                            // single image
                                            else
                                            {
                                                var imageUrl = post.Elements("photo-url").Where(photo_url =>
                                                        photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).FirstOrDefault().Value;

                                                if (blog.SkipGif == true && imageUrl.EndsWith(".gif"))
                                                    continue;

                                                if (blog.ForceSize)
                                                {
                                                    StringBuilder sb = new StringBuilder(imageUrl);
                                                    imageUrl = sb
                                                      .Replace("_1280", "_" + shellService.Settings.ImageSize.ToString())
                                                      .Replace("_540", "_" + shellService.Settings.ImageSize.ToString())
                                                      .Replace("_500", "_" + shellService.Settings.ImageSize.ToString())
                                                      .Replace("_400", "_" + shellService.Settings.ImageSize.ToString())
                                                      .Replace("_250", "_" + shellService.Settings.ImageSize.ToString())
                                                      .Replace("_100", "_" + shellService.Settings.ImageSize.ToString())
                                                      .Replace("_75sq", "_" + shellService.Settings.ImageSize.ToString())
                                                      .ToString();
                                                }

                                                Interlocked.Increment(ref totalDownloads);
                                                Interlocked.Increment(ref photos);
                                                Monitor.Enter(images);
                                                images.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                                                Monitor.Exit(images);
                                            }
                                        }
                                        // check for inline images
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "regular" && posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                                        {
                                            if (post.Element("regular-body").Value.Contains("tumblr_inline"))
                                            {
                                                Regex regex = new Regex("<img src=\"(.*?)\"");
                                                foreach (Match match in regex.Matches(post.Element("regular-body")?.Value))
                                                {
                                                    var imageUrl = match.Groups[1].Value;
                                                    if (blog.ForceSize)
                                                    {
                                                        StringBuilder sb = new StringBuilder(imageUrl);
                                                        imageUrl = sb
                                                          .Replace("_1280", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_540", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_500", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_400", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_250", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_100", "_" + shellService.Settings.ImageSize.ToString())
                                                          .Replace("_75sq", "_" + shellService.Settings.ImageSize.ToString())
                                                          .ToString();
                                                    }

                                                    Interlocked.Increment(ref totalDownloads);
                                                    Monitor.Enter(images);
                                                    images.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                                                    Monitor.Exit(images);
                                                }
                                            }
                                        }
                                    }
                                    if (blog.DownloadVideo == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "video" && posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                                        {
                                            var videoUrl = post.Descendants("video-player").Where(x => x.Value.Contains("<source src=")).Select(result =>
                                                System.Text.RegularExpressions.Regex.Match(
                                                result.Value, "<source src=\"(.*)\" type=\"video/mp4\">").Groups[1].Value).ToList();

                                            foreach (string video in videoUrl)
                                            {
                                                if (shellService.Settings.VideoSize == 1080)
                                                {
                                                    Interlocked.Increment(ref totalDownloads);
                                                    Monitor.Enter(images);
                                                    images.Add(Tuple.Create(video.Replace("/480", "") + ".mp4", "Video", post.Attribute("id").Value));
                                                    Monitor.Exit(images);
                                                }
                                                else if (shellService.Settings.VideoSize == 480)
                                                {
                                                    Interlocked.Increment(ref totalDownloads);
                                                    Monitor.Enter(images);
                                                    images.Add(Tuple.Create("https://vt.tumblr.com/" + video.Replace("/480", "").Split('/').Last() + "_480.mp4", "Video", post.Attribute("id").Value));
                                                    Monitor.Exit(images);
                                                }
                                            }
                                        }
                                    }
                                    if (blog.DownloadAudio == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "audio"))
                                        {
                                            var audioUrl = post.Descendants("audio-player").Where(x => x.Value.Contains("src=")).Select(result =>
                                                System.Text.RegularExpressions.Regex.Match(
                                                result.Value, "src=\"(.*)\" height").Groups[1].Value).ToList();

                                            foreach (string audiofile in audioUrl)
                                            {
                                                Interlocked.Increment(ref totalDownloads);
                                                Monitor.Enter(images);
                                                images.Add(Tuple.Create(WebUtility.UrlDecode(audiofile), "Audio", post.Attribute("id").Value));
                                                Monitor.Exit(images);
                                            }

                                        }
                                    }
                                    if (blog.DownloadText == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "regular" && posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                                        {
                                            string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Title, post.Element("regular-title")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Body, post.Element("regular-body")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                                                Environment.NewLine;
                                            Interlocked.Increment(ref totalDownloads);
                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Text", post.Attribute("id").Value));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.DownloadQuote == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "quote" && posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                                        {
                                            string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.Element("quote-text")?.Value) +
                                                Environment.NewLine + post.Element("quote-source")?.Value +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                                                Environment.NewLine;
                                            Interlocked.Increment(ref totalDownloads);
                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Quote", post.Attribute("id").Value));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.DownloadLink == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "link" && posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                                        {
                                            string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Link, post.Element("link-text")?.Value) +
                                                Environment.NewLine + post.Element("link-url")?.Value +
                                                Environment.NewLine + post.Element("link-description")?.Value +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                                                Environment.NewLine;
                                            Interlocked.Increment(ref totalDownloads);
                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Link", post.Attribute("id").Value));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.DownloadConversation == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "conversation" && posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                                        {
                                            string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Conversation, post.Element("conversation-text")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                                                Environment.NewLine;
                                            Interlocked.Increment(ref totalDownloads);
                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Conversation", post.Attribute("id").Value));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.CreatePhotoMeta == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "photo" && posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                                        {
                                            string imageUrl = "";
                                            // photoset
                                            if (post.Descendants("photoset").Count() > 0)
                                            {
                                                List<string> imageUrls = new List<string>();
                                                foreach (var photo in post.Descendants("photoset").Descendants("photo"))
                                                {
                                                    var singleImageUrl = photo.Elements("photo-url").Where(photo_url =>
                                                        photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).FirstOrDefault().Value;

                                                    if (blog.SkipGif == true && imageUrl.EndsWith(".gif"))
                                                        continue;
                                                    imageUrls.Add(singleImageUrl);
                                                }
                                                // imageUrl = imageUrls.Aggregate((current, next) => current + ", " + next);
                                                imageUrl = string.Join(", ", imageUrls.ToArray());
                                            }
                                            // single image
                                            {
                                                imageUrl = post.Elements("photo-url").Where(photo_url =>
                                                        photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).FirstOrDefault().Value;

                                                if (blog.SkipGif == true && imageUrl.EndsWith(".gif"))
                                                    continue;

                                                string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                                                    Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                                                    Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                                                    Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.PhotoUrl, imageUrl) +
                                                    Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.PhotoCaption, post.Element("photo-caption")?.Value) +
                                                    Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                                                    Environment.NewLine;
                                                Interlocked.Increment(ref photoMetas);
                                                Interlocked.Increment(ref totalDownloads);
                                                Monitor.Enter(images);
                                                images.Add(Tuple.Create(textBody, "PhotoMeta", post.Attribute("id").Value));
                                                Monitor.Exit(images);
                                            }
                                        }
                                    }
                                    if (blog.CreateVideoMeta == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "video" && posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                                        {
                                            string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.VideoPlayer, post.Element("video-player")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                                                Environment.NewLine;
                                            Interlocked.Increment(ref videoMetas);
                                            Interlocked.Increment(ref totalDownloads);
                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "VideoMeta", post.Attribute("id").Value));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.CreateAudioMeta == true)
                                    {
                                        foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "audio"))
                                        {
                                            string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.AudioCaption, post.Element("audio-caption")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Artist, post.Element("id3-artist")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Title, post.Element("id3-title")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.Element("id3-track")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Album, post.Element("id3-album")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Id3Year, post.Element("id3-year")?.Value) +
                                                Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                                                Environment.NewLine;
                                            Interlocked.Increment(ref audioMetas);
                                            Interlocked.Increment(ref totalDownloads);
                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "AudioMeta", post.Attribute("id").Value));
                                            Monitor.Exit(images);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Data);
                            }

                            numberOfPostsCrawled += 50;
                            var newProgress = new Datamodels.DownloadProgress();
                            newProgress.Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressGetUrl, numberOfPostsCrawled, totalPosts);
                            progress.Report(newProgress);
                        }
                );

            blog.Posts = (uint)totalPosts;
            blog.Photos = (uint)photos;
            blog.Videos = (uint)videos;
            blog.Audios = (uint)audios;
            blog.Texts = (uint)texts;
            blog.Conversations = (uint)conversations;
            blog.Quotes = (uint)quotes;
            blog.NumberOfLinks = (uint)links;
            blog.PhotoMetas = (uint)photoMetas;
            blog.VideoMetas = (uint)videoMetas;
            blog.AudioMetas = (uint)audioMetas;

            return Tuple.Create((uint)totalDownloads, images);
        }

        public string ExtractBlogname(string url)
        {
            string[] source = url.Split(new char[] { '.' });
            if ((source.Count<string>() >= 3) && source[0].StartsWith("http://", true, null))
            {
                return source[0].Replace("http://", string.Empty);
            }
            else if ((source.Count<string>() >= 3) && source[0].StartsWith("https://", true, null))
            {
                return source[0].Replace("https://", string.Empty);
            }
            return null;
        }

        private bool Download(TumblrBlog blog, string fileLocation, string url, IProgress<DataModels.DownloadProgress> progress, object lockObject, bool locked, ref int counter, ref int totalCounter)
        {
            Monitor.Enter(lockObject, ref locked);
            if (blog.Links.Contains(url.Split('/').Last()))
            {
                Monitor.Exit(lockObject);
                return false;
            }
            else
            {
                Monitor.Exit(lockObject);
                try
                {
                    var newProgress = new DataModels.DownloadProgress();
                    newProgress.Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressDownloadImage, url.Split('/').Last()); ;
                    progress.Report(newProgress);

                    using (var stream = ThrottledStream.ReadFromURLIntoStream(url, (shellService.Settings.Bandwidth / shellService.Settings.ParallelImages), shellService.Settings.TimeOut, shellService.Settings.ProxyHost, shellService.Settings.ProxyPort))
                        ThrottledStream.SaveStreamToDisk(stream, fileLocation);
                    Interlocked.Increment(ref counter);
                    Interlocked.Increment(ref totalCounter);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private bool Download(TumblrBlog blog, string fileLocation, string postId, string text, IProgress<DataModels.DownloadProgress> progress, object lockObject, bool locked, ref int counter, ref int totalCounter)
        {
            Monitor.Enter(lockObject, ref locked);
            if (blog.Links.Contains(postId))
            {
                Monitor.Exit(lockObject);
                return false;
            }
            else
            {
                try
                {
                    var newProgress = new DataModels.DownloadProgress();
                    newProgress.Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressDownloadImage, "Post: " + postId);
                    progress.Report(newProgress);

                    using (StreamWriter sw = new StreamWriter(fileLocation, true))
                    {
                        sw.WriteLine(text);
                    }
                    Interlocked.Increment(ref counter);
                    Interlocked.Increment(ref totalCounter);
                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    Monitor.Exit(lockObject);
                }
            }
        }

        private void OnClipboardContentChanged(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {

                // Count each whitespace as new url
                string[] urls = Clipboard.GetText().ToString().Split(new char[0]);

                foreach (string url in urls)
                {
                    if (Validator.IsValidTumblrUrl(url))
                    {
                        Task.Factory.StartNew(() =>
                        {
                            return AddBlogAsync(url);
                        },
                        TaskCreationOptions.LongRunning);
                    }
                }
            }
        }

        private void ListenClipboard()
        {
            if (shellService.Settings.CheckClipboard)
            {
                shellService.ClipboardMonitor.OnClipboardContentChanged += OnClipboardContentChanged;
                return;
            }
            shellService.ClipboardMonitor.OnClipboardContentChanged -= OnClipboardContentChanged;
        }

        private void CrawlerServicePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(crawlerService.NewBlogUrl))
            {
                addBlogCommand.RaiseCanExecuteChanged();
            }
        }

        private void ManagerViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ManagerViewModel.SelectedBlogFile))
            {
                UpdateCommands();
            }
        }

        private void UpdateCommands()
        {
            enqueueSelectedCommand.RaiseCanExecuteChanged();
            removeBlogCommand.RaiseCanExecuteChanged();
            showFilesCommand.RaiseCanExecuteChanged();
        }
    }
}