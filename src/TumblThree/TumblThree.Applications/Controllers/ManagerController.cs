using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Windows;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.Crawler;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Controllers
{
    [Export]
    internal class ManagerController
    {
        #region Delegates

        public delegate void BlogManagerFinishedLoadingHandler(object sender, EventArgs e);

        #endregion

        private readonly AsyncDelegateCommand addBlogCommand;
        private readonly DelegateCommand autoDownloadCommand;
        private readonly ICrawlerService crawlerService;
        private readonly DelegateCommand enqueueSelectedCommand;
        private readonly DelegateCommand listenClipboardCommand;
        private readonly AsyncDelegateCommand loadLibraryCommand;
        private readonly AsyncDelegateCommand loadAllDatabasesCommand;

        private readonly object lockObject = new object();
        private readonly IManagerService managerService;
        private readonly Lazy<ManagerViewModel> managerViewModel;
        private readonly DelegateCommand removeBlogCommand;
        private readonly ISelectionService selectionService;
        private readonly IShellService shellService;
        private readonly ISettingsService settingsService;
        private readonly DelegateCommand showDetailsCommand;
        private readonly DelegateCommand showFilesCommand;
        private readonly DelegateCommand visitBlogCommand;

        [ImportingConstructor]
        public ManagerController(IShellService shellService, ISelectionService selectionService, ICrawlerService crawlerService, ISettingsService settingsService,
            IManagerService managerService, ICrawlerFactory crawlerFactory, IBlogFactory blogFactory, ITumblrBlogDetector tumblrBlogDetector, Lazy<ManagerViewModel> managerViewModel)
        {
            this.shellService = shellService;
            this.selectionService = selectionService;
            this.crawlerService = crawlerService;
            this.managerService = managerService;
            this.managerViewModel = managerViewModel;
            this.settingsService = settingsService;
            CrawlerFactory = crawlerFactory;
            BlogFactory = blogFactory;
            TumblrBlogDetector = tumblrBlogDetector;
            addBlogCommand = new AsyncDelegateCommand(AddBlog, CanAddBlog);
            removeBlogCommand = new DelegateCommand(RemoveBlog, CanRemoveBlog);
            showFilesCommand = new DelegateCommand(ShowFiles, CanShowFiles);
            visitBlogCommand = new DelegateCommand(VisitBlog, CanVisitBlog);
            enqueueSelectedCommand = new DelegateCommand(EnqueueSelected, CanEnqueueSelected);
            loadLibraryCommand = new AsyncDelegateCommand(LoadLibrary, CanLoadLibrary);
            loadAllDatabasesCommand = new AsyncDelegateCommand(LoadAllDatabases, CanLoadAllDatbases);
            listenClipboardCommand = new DelegateCommand(ListenClipboard);
            autoDownloadCommand = new DelegateCommand(EnqueueAutoDownload, CanEnqueueAutoDownload);
            showDetailsCommand = new DelegateCommand(ShowDetailsCommand);
        }

        private ManagerViewModel ManagerViewModel
        {
            get { return managerViewModel.Value; }
        }

        public ManagerSettings ManagerSettings { get; set; }

        public QueueManager QueueManager { get; set; }

        public ICrawlerFactory CrawlerFactory { get; set; }

        public IBlogFactory BlogFactory { get; set; }

        public ITumblrBlogDetector TumblrBlogDetector { get; set; }

        public event BlogManagerFinishedLoadingHandler BlogManagerFinishedLoading;

        public async Task Initialize()
        {
            crawlerService.AddBlogCommand = addBlogCommand;
            crawlerService.RemoveBlogCommand = removeBlogCommand;
            crawlerService.ShowFilesCommand = showFilesCommand;
            crawlerService.EnqueueSelectedCommand = enqueueSelectedCommand;
            crawlerService.LoadLibraryCommand = loadLibraryCommand;
            crawlerService.LoadAllDatabasesCommand = loadAllDatabasesCommand;
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

            if (shellService.Settings.CheckClipboard)
            {
                shellService.ClipboardMonitor.OnClipboardContentChanged += OnClipboardContentChanged;
            }

            await LoadLibrary();
            await LoadAllDatabases();
        }

        public void Shutdown()
        {
        }

        private void QueueItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

        private async Task LoadLibrary()
        {
            Logger.Verbose("ManagerController.LoadLibrary:Start");
            managerService.BlogFiles.Clear();
            string path = Path.Combine(shellService.Settings.DownloadLocation, "Index");

            try
            {
                if (Directory.Exists(path))
                {
                    {
                        IReadOnlyList<IBlog> files = await GetIBlogsAsync(path);
                        foreach (IBlog file in files)
                        {
                            managerService.BlogFiles.Add(file);
                        }

                        BlogManagerFinishedLoading?.Invoke(this, EventArgs.Empty);

                        if (shellService.Settings.CheckOnlineStatusAtStartup)
                        {
                            foreach (IBlog blog in files)
                            {
                                ICrawler downloader = CrawlerFactory.GetCrawler(blog, new CancellationToken(), new PauseToken(), new Progress<DownloadProgress>(), shellService,
                                    crawlerService, managerService);
                                await downloader.IsBlogOnlineAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Verbose("ManagerController:LoadLibrary: {0}", ex);
                shellService.ShowError(ex, Resources.CouldNotLoadLibrary, ex.Data["Filename"]);
            }
            Logger.Verbose("ManagerController.LoadLibrary:End");
        }

        //TODO: Refactor and extract blog loading.
        private Task<IReadOnlyList<IBlog>> GetIBlogsAsync(string directory)
        {
            return Task.Run(() => GetIBlogsCore(directory));
        }

        private IReadOnlyList<IBlog> GetIBlogsCore(string directory)
        {
            Logger.Verbose("ManagerController:GetFilesCore Start");

            var blogs = new List<IBlog>();

            string[] supportedFileTypes = Enum.GetNames(typeof(BlogTypes)).ToArray();

            foreach (string filename in Directory.GetFiles(directory, "*").Where(
                fileName => supportedFileTypes.Any(fileName.Contains) &&
                            !fileName.Contains("_files")))
            {
                //TODO: Refactor
                if (filename.EndsWith(BlogTypes.tumblr.ToString()))
                    blogs.Add(new TumblrBlog().Load(filename));
                if (filename.EndsWith(BlogTypes.tmblrpriv.ToString()))
                    blogs.Add(new TumblrHiddenBlog().Load(filename));
                if (filename.EndsWith(BlogTypes.tlb.ToString()))
                    blogs.Add(new TumblrLikedByBlog().Load(filename));
                if (filename.EndsWith(BlogTypes.tumblrsearch.ToString()))
                    blogs.Add(new TumblrSearchBlog().Load(filename));
                if (filename.EndsWith(BlogTypes.tumblrtagsearch.ToString()))
                    blogs.Add(new TumblrTagSearchBlog().Load(filename));
            }
            Logger.Verbose("ManagerController.GetFilesCore End");

            return blogs;
        }

        private Task<IReadOnlyList<IFiles>> GetIFilesAsync(string directory)
        {
            return Task.Run(() => GetIFilesCore(directory));
        }

        private IReadOnlyList<IFiles> GetIFilesCore(string directory)
        {
            Logger.Verbose("ManagerController:GetFilesCore Start");

            var blogs = new List<IFiles>();

            string[] supportedFileTypes = Enum.GetNames(typeof(BlogTypes)).ToArray();

            foreach (string filename in Directory.GetFiles(directory, "*").Where(
                fileName => supportedFileTypes.Any(fileName.Contains) &&
                            fileName.Contains("_files")))
            {
                //TODO: Refactor
                blogs.Add(new Files().Load(filename));
            }
            Logger.Verbose("ManagerController.GetFilesCore End");

            return blogs;
        }

        private async Task LoadAllDatabases()
        {
            Logger.Verbose("ManagerController.LoadDatabasesGloballyAsync:Start");
            managerService.Databases.Clear();
            if (shellService.Settings.LoadAllDatabases)
            {
                string path = Path.Combine(shellService.Settings.DownloadLocation, "Index");

                try
                {
                    if (Directory.Exists(path))
                    {
                        {
                            IReadOnlyList<IFiles> databases = await GetIFilesAsync(path);
                            foreach (IFiles database in databases)
                            {
                                managerService.Databases.Add(database);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Verbose("ManagerController:LoadDatabasesGloballyAsync: {0}", ex);
                    shellService.ShowError(ex, Resources.CouldNotLoadLibrary, ex.Data["Filename"]);
                }
                Logger.Verbose("ManagerController.LoadDatabasesGloballyAsync:End");
            }
        }

        private bool CanLoadLibrary()
        {
            return !crawlerService.IsCrawl;
        }

        private bool CanLoadAllDatbases()
        {
            return !crawlerService.IsCrawl;
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
        }

        private bool CanEnqueueAutoDownload()
        {
            return managerService.BlogFiles.Any();
        }

        private void EnqueueAutoDownload()
        {
            if (shellService.Settings.BlogType == shellService.Settings.BlogTypes.ElementAtOrDefault(0))
            {
            }
            if (shellService.Settings.BlogType == shellService.Settings.BlogTypes.ElementAtOrDefault(1))
            {
                Enqueue(managerService.BlogFiles.Where(blog => blog.Online).ToArray());
            }
            if (shellService.Settings.BlogType == shellService.Settings.BlogTypes.ElementAtOrDefault(2))
            {
                Enqueue(
                    managerService.BlogFiles.Where(blog => blog.Online && blog.LastCompleteCrawl != new DateTime(0L, DateTimeKind.Utc)).ToArray());
            }
            if (shellService.Settings.BlogType == shellService.Settings.BlogTypes.ElementAtOrDefault(3))
            {
                Enqueue(
                    managerService.BlogFiles.Where(blog => blog.Online && blog.LastCompleteCrawl == new DateTime(0L, DateTimeKind.Utc)).ToArray());
            }

            if (crawlerService.IsCrawl && crawlerService.IsPaused)
            {
                crawlerService.ResumeCommand.CanExecute(null);
                crawlerService.ResumeCommand.Execute(null);
            }
            else if (!crawlerService.IsCrawl)
            {
                crawlerService.CrawlCommand.CanExecute(null);
                crawlerService.CrawlCommand.Execute(null);
            }
        }

        private bool CanAddBlog()
        {
            return BlogFactory.IsValidTumblrBlogUrl(crawlerService.NewBlogUrl);
        }

        private async Task AddBlog()
        {
            await AddBlogAsync(null);
        }

        private bool CanRemoveBlog()
        {
            return ManagerViewModel.SelectedBlogFile != null;
        }

        private void RemoveBlog()
        {
            IBlog[] blogs = selectionService.SelectedBlogFiles.ToArray();
            RemoveBlog(blogs);
        }

        private void RemoveBlog(IEnumerable<IBlog> blogs)
        {
            foreach (IBlog blog in blogs)
            {
                if (!shellService.Settings.DeleteOnlyIndex)
                {
                    try
                    {
                        string blogPath = blog.DownloadLocation();
                        Directory.Delete(blogPath, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("ManagerController:RemoveBlog: {0}", ex);
                        shellService.ShowError(ex, Resources.CouldNotRemoveBlog, blog.Name);
                        return;
                    }
                }

                string indexFile = Path.Combine(blog.Location, blog.Name) + "." + blog.BlogType;
                try
                {
                    File.Delete(indexFile);
                    File.Delete(blog.ChildId);
                }
                catch (Exception ex)
                {
                    Logger.Error("ManagerController:RemoveBlog: {0}", ex);
                    shellService.ShowError(ex, Resources.CouldNotRemoveBlogIndex, blog.Name);
                    return;
                }

                managerService.BlogFiles.Remove(blog);
                QueueManager.RemoveItems(QueueManager.Items.Where(item => item.Blog.Equals(blog)));
            }
        }
        private bool CanShowFiles()
        {
            return ManagerViewModel.SelectedBlogFile != null;
        }

        private void ShowFiles()
        {
            foreach (IBlog blog in selectionService.SelectedBlogFiles.ToArray())
            {
                System.Diagnostics.Process.Start("explorer.exe", blog.DownloadLocation());
            }
        }

        private bool CanVisitBlog()
        {
            return ManagerViewModel.SelectedBlogFile != null;
        }

        private void VisitBlog()
        {
            foreach (IBlog blog in selectionService.SelectedBlogFiles.ToArray())
            {
                System.Diagnostics.Process.Start(blog.Url);
            }
        }

        private void ShowDetailsCommand()
        {
            shellService.ShowDetailsView();
        }

        private async Task AddBlogAsync(string blogUrl)
        {
            if (string.IsNullOrEmpty(blogUrl))
            {
                blogUrl = crawlerService.NewBlogUrl;
            }

            IBlog blog;
            try
            {
                blog = BlogFactory.GetBlog(blogUrl, Path.Combine(shellService.Settings.DownloadLocation, "Index"));
            }
            catch (ArgumentException)
            {
                return;
            }

            if (await TumblrBlogDetector.IsHiddenTumblrBlog(blog.Url))
            {
                blog = PromoteTumblrBlogToHiddenBlog(blog);
            }

            lock (lockObject)
            {
                if (managerService.BlogFiles.Any(blogs => blogs.Name.Equals(blog.Name) && blogs.BlogType.Equals(blog.BlogType)))
                {
                    shellService.ShowError(null, Resources.BlogAlreadyExist, blog.Name);
                    return;
                }

                if (blog.Save())
                {
                    AddToManager(blog);
                }
            }

            blog = settingsService.TransferGlobalSettingsToBlog(blog);
            ICrawler crawler = CrawlerFactory.GetCrawler(blog, new CancellationToken(), new PauseToken(), new Progress<DownloadProgress>(), shellService, crawlerService, managerService);
            await crawler.UpdateMetaInformationAsync();
        }

        private void AddToManager(IBlog blog)
        {
            QueueOnDispatcher.CheckBeginInvokeOnUI((Action)(() => managerService.BlogFiles.Add(blog)));
            if (shellService.Settings.LoadAllDatabases)            
                managerService.Databases.Add(new Files().Load(blog.ChildId));
        }

        private IBlog PromoteTumblrBlogToHiddenBlog(IBlog blog)
        {
            RemoveBlog(new[] { blog } );
            blog = TumblrHiddenBlog.Create(blog.Url, Path.Combine(shellService.Settings.DownloadLocation, "Index"));
            return blog;
        }

        private void OnClipboardContentChanged(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                // Count each whitespace as new url
                string[] urls = Clipboard.GetText().Split();

                Task.Run(() => { Task addBlogBatchedTask = AddBlogBatchedAsync(urls); });
            }
        }

        private async Task AddBlogBatchedAsync(IEnumerable<string> urls)
        {
            var semaphoreSlim = new SemaphoreSlim(25);
            IEnumerable<Task> tasks = urls.Select(async url =>
            {
                try
                {
                    await semaphoreSlim.WaitAsync();
                    await AddBlogAsync(url);
                }
                catch { }
                finally
                {
                    semaphoreSlim.Release();
                }
            });
            await Task.WhenAll(tasks);
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
