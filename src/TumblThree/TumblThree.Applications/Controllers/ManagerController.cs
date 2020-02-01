using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Waf.Applications.Services;
using System.Windows.Forms;
using TumblThree.Applications.Crawler;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;
using TumblThree.Domain.Queue;

using Clipboard = System.Windows.Clipboard;

namespace TumblThree.Applications.Controllers
{
    [Export]
    internal class ManagerController
    {
        private readonly IBlogFactory blogFactory;
        private readonly ICrawlerService crawlerService;
        private readonly IClipboardService clipboardService;
        private readonly ICrawlerFactory crawlerFactory;
        private readonly IManagerService managerService;
        private readonly Lazy<ManagerViewModel> managerViewModel;
        private readonly IMessageService messageService;
        private readonly ISelectionService selectionService;
        private readonly IShellService shellService;
        private readonly ISettingsService settingsService;
        private readonly ITumblrBlogDetector tumblrBlogDetector;

        private readonly AsyncDelegateCommand checkStatusCommand;
        private readonly DelegateCommand copyUrlCommand;
        private readonly DelegateCommand checkIfDatabasesCompleteCommand;
        private readonly AsyncDelegateCommand importBlogsCommand;
        private readonly AsyncDelegateCommand addBlogCommand;
        private readonly DelegateCommand autoDownloadCommand;
        private readonly DelegateCommand enqueueSelectedCommand;
        private readonly DelegateCommand listenClipboardCommand;
        private readonly AsyncDelegateCommand loadLibraryCommand;
        private readonly AsyncDelegateCommand loadAllDatabasesCommand;
        private readonly DelegateCommand removeBlogCommand;
        private readonly DelegateCommand showDetailsCommand;
        private readonly DelegateCommand showFilesCommand;
        private readonly DelegateCommand visitBlogCommand;
        private readonly DelegateCommand visitBlogOnTumbexCommand;

        private SemaphoreSlim addBlogSemaphoreSlim = new SemaphoreSlim(1);
        private readonly object lockObject = new object();

        #region Delegates

        public delegate void BlogManagerFinishedLoadingLibraryHandler(object sender, EventArgs e);

        public delegate void BlogManagerFinishedLoadingDatabasesHandler(object sender, EventArgs e);

        #endregion

        [ImportingConstructor]
        public ManagerController(IShellService shellService, ISelectionService selectionService, ICrawlerService crawlerService,
            ISettingsService settingsService, IClipboardService clipboardService, IManagerService managerService,
            ICrawlerFactory crawlerFactory, IBlogFactory blogFactory, ITumblrBlogDetector tumblrBlogDetector,
            IMessageService messageService, Lazy<ManagerViewModel> managerViewModel)
        {
            this.shellService = shellService;
            this.selectionService = selectionService;
            this.clipboardService = clipboardService;
            this.crawlerService = crawlerService;
            this.managerService = managerService;
            this.managerViewModel = managerViewModel;
            this.settingsService = settingsService;
            this.messageService = messageService;
            this.crawlerFactory = crawlerFactory;
            this.blogFactory = blogFactory;
            this.tumblrBlogDetector = tumblrBlogDetector;
            importBlogsCommand = new AsyncDelegateCommand(ImportBlogs);
            addBlogCommand = new AsyncDelegateCommand(AddBlog, CanAddBlog);
            removeBlogCommand = new DelegateCommand(RemoveBlog, CanRemoveBlog);
            showFilesCommand = new DelegateCommand(ShowFiles, CanShowFiles);
            visitBlogCommand = new DelegateCommand(VisitBlog, CanVisitBlog);
            visitBlogOnTumbexCommand = new DelegateCommand(VisitBlogOnTumbex, CanVisitBlog);
            enqueueSelectedCommand = new DelegateCommand(EnqueueSelected, CanEnqueueSelected);
            loadLibraryCommand = new AsyncDelegateCommand(LoadLibraryAsync, CanLoadLibrary);
            loadAllDatabasesCommand = new AsyncDelegateCommand(LoadAllDatabasesAsync, CanLoadAllDatbases);
            checkIfDatabasesCompleteCommand = new DelegateCommand(CheckIfDatabasesComplete, CanCheckIfDatabasesComplete);
            listenClipboardCommand = new DelegateCommand(ListenClipboard);
            autoDownloadCommand = new DelegateCommand(EnqueueAutoDownload, CanEnqueueAutoDownload);
            showDetailsCommand = new DelegateCommand(ShowDetailsCommand);
            copyUrlCommand = new DelegateCommand(CopyUrl, CanCopyUrl);
            checkStatusCommand = new AsyncDelegateCommand(CheckStatusAsync, CanCheckStatus);
        }

        private ManagerViewModel ManagerViewModel => managerViewModel.Value;

        public ManagerSettings ManagerSettings { get; set; }

        public QueueManager QueueManager { get; set; }

        public event BlogManagerFinishedLoadingLibraryHandler BlogManagerFinishedLoadingLibrary;

        public event BlogManagerFinishedLoadingDatabasesHandler BlogManagerFinishedLoadingDatabases;

        public async Task InitializeAsync()
        {
            crawlerService.ImportBlogsCommand = importBlogsCommand;
            crawlerService.AddBlogCommand = addBlogCommand;
            crawlerService.RemoveBlogCommand = removeBlogCommand;
            crawlerService.ShowFilesCommand = showFilesCommand;
            crawlerService.EnqueueSelectedCommand = enqueueSelectedCommand;
            crawlerService.LoadLibraryCommand = loadLibraryCommand;
            crawlerService.LoadAllDatabasesCommand = loadAllDatabasesCommand;
            crawlerService.CheckIfDatabasesCompleteCommand = checkIfDatabasesCompleteCommand;
            crawlerService.AutoDownloadCommand = autoDownloadCommand;
            crawlerService.ListenClipboardCommand = listenClipboardCommand;
            crawlerService.PropertyChanged += CrawlerServicePropertyChanged;

            ManagerViewModel.ShowFilesCommand = showFilesCommand;
            ManagerViewModel.VisitBlogCommand = visitBlogCommand;
            ManagerViewModel.VisitBlogOnTumbexCommand = visitBlogOnTumbexCommand;
            ManagerViewModel.ShowDetailsCommand = showDetailsCommand;
            ManagerViewModel.CopyUrlCommand = copyUrlCommand;
            ManagerViewModel.CheckStatusCommand = checkStatusCommand;

            ManagerViewModel.PropertyChanged += ManagerViewModelPropertyChanged;

            ManagerViewModel.QueueItems = QueueManager.Items;
            QueueManager.Items.CollectionChanged += QueueItemsCollectionChanged;
            ManagerViewModel.QueueItems.CollectionChanged += ManagerViewModel.QueueItemsCollectionChanged;
            BlogManagerFinishedLoadingLibrary += OnBlogManagerFinishedLoadingLibrary;
            BlogManagerFinishedLoadingDatabases += OnBlogManagerFinishedLoadingDatabases;

            shellService.ContentView = ManagerViewModel.View;

            // Refresh command availability on selection change.
            ManagerViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName != nameof(ManagerViewModel.SelectedBlogFile))
                    return;

                showFilesCommand.RaiseCanExecuteChanged();
                visitBlogCommand.RaiseCanExecuteChanged();
                visitBlogOnTumbexCommand.RaiseCanExecuteChanged();
                showDetailsCommand.RaiseCanExecuteChanged();
                copyUrlCommand.RaiseCanExecuteChanged();
                checkStatusCommand.RaiseCanExecuteChanged();
            };

            if (shellService.Settings.CheckClipboard)
            {
                shellService.ClipboardMonitor.OnClipboardContentChanged += OnClipboardContentChanged;
            }

            await LoadDataBasesAsync();
        }

        public void Shutdown()
        {
        }

        private void OnBlogManagerFinishedLoadingLibrary(object sender, EventArgs e) =>
            crawlerService.LibraryLoaded.SetResult(true);

        private void OnBlogManagerFinishedLoadingDatabases(object sender, EventArgs e) =>
            crawlerService.DatabasesLoaded.SetResult(true);

        private void QueueItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add | e.Action == NotifyCollectionChangedAction.Remove)
                ManagerViewModel.QueueItems = QueueManager.Items;
        }

        private async Task LoadDataBasesAsync()
        {
            // TODO: Methods have side effects!
            // They remove blogs from the blog manager.
            await LoadLibraryAsync();
            await LoadAllDatabasesAsync();
            CheckIfDatabasesComplete();
            await CheckBlogsOnlineStatusAsync();
        }

        private async Task LoadLibraryAsync()
        {
            Logger.Verbose("ManagerController.LoadLibrary:Start");
            managerService.BlogFiles.Clear();
            string path = Path.Combine(shellService.Settings.DownloadLocation, "Index");

            if (Directory.Exists(path))
            {
                IReadOnlyList<IBlog> files = await GetIBlogsAsync(path);
                foreach (IBlog file in files)
                {
                    managerService.BlogFiles.Add(file);
                }
            }

            BlogManagerFinishedLoadingLibrary?.Invoke(this, EventArgs.Empty);
            Logger.Verbose("ManagerController.LoadLibrary:End");
        }

        //TODO: Refactor and extract blog loading.
        private Task<IReadOnlyList<IBlog>> GetIBlogsAsync(string directory) => Task.Run(() => GetIBlogsCore(directory));

        private IReadOnlyList<IBlog> GetIBlogsCore(string directory)
        {
            Logger.Verbose("ManagerController:GetIBlogsCore Start");

            var blogs = new List<IBlog>();
            var failedToLoadBlogs = new List<string>();

            string[] supportedFileTypes = Enum.GetNames(typeof(BlogTypes)).ToArray();

            foreach (string filename in Directory.GetFiles(directory, "*").Where(
                fileName => supportedFileTypes.Any(fileName.Contains) &&
                            !fileName.Contains("_files")))
            {
                //TODO: Refactor
                try
                {
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
                catch (SerializationException ex)
                {
                    failedToLoadBlogs.Add(ex.Data["Filename"].ToString());
                }
            }

            if (failedToLoadBlogs.Any())
            {
                string failedBlogNames = failedToLoadBlogs.Aggregate((a, b) => a + ", " + b);
                Logger.Verbose("ManagerController:GetIBlogsCore: {0}", failedBlogNames);
                shellService.ShowError(new SerializationException(), Resources.CouldNotLoadLibrary, failedBlogNames);
            }

            Logger.Verbose("ManagerController.GetIBlogsCore End");

            return blogs;
        }

        private async Task LoadAllDatabasesAsync()
        {
            Logger.Verbose("ManagerController.LoadAllDatabasesAsync:Start");
            managerService.ClearDatabases();
            string path = Path.Combine(shellService.Settings.DownloadLocation, "Index");

            if (Directory.Exists(path))
            {
                IReadOnlyList<IFiles> databases = await GetIFilesAsync(path);
                foreach (IFiles database in databases)
                {
                    managerService.AddDatabase(database);
                }
            }

            BlogManagerFinishedLoadingDatabases?.Invoke(this, EventArgs.Empty);
            Logger.Verbose("ManagerController.LoadAllDatabasesAsync:End");
        }

        private Task<IReadOnlyList<IFiles>> GetIFilesAsync(string directory) => Task.Run(() => GetIFilesCore(directory));

        private IReadOnlyList<IFiles> GetIFilesCore(string directory)
        {
            Logger.Verbose("ManagerController:GetFilesCore Start");

            var databases = new List<IFiles>();
            var failedToLoadDatabases = new List<string>();

            string[] supportedFileTypes = Enum.GetNames(typeof(BlogTypes)).ToArray();

            foreach (string filename in Directory.GetFiles(directory, "*").Where(
                fileName => supportedFileTypes.Any(fileName.Contains) &&
                            fileName.Contains("_files")))
            {
                //TODO: Refactor
                try
                {
                    IFiles database = new Files().Load(filename);
                    if (shellService.Settings.LoadAllDatabases)
                        databases.Add(database);
                }
                catch (SerializationException ex)
                {
                    failedToLoadDatabases.Add(ex.Data["Filename"].ToString());
                }
            }

            if (failedToLoadDatabases.Any())
            {
                IEnumerable<IBlog> blogs = managerService.BlogFiles;
                IEnumerable<IBlog> failedToLoadBlogs = blogs.Where(blog => failedToLoadDatabases.Contains(blog.ChildId));

                string failedBlogNames = failedToLoadDatabases.Aggregate((a, b) => a + ", " + b);
                Logger.Verbose("ManagerController:GetIFilesCore: {0}", failedBlogNames);
                shellService.ShowError(new SerializationException(), Resources.CouldNotLoadLibrary, failedBlogNames);

                foreach (IBlog failedToLoadBlog in failedToLoadBlogs)
                {
                    managerService.BlogFiles.Remove(failedToLoadBlog);
                }
            }

            Logger.Verbose("ManagerController.GetFilesCore End");

            return databases;
        }

        private void CheckIfDatabasesComplete()
        {
            IEnumerable<IBlog> blogs = managerService.BlogFiles;
            List<IBlog> incompleteBlogs = blogs.Where(blog => !File.Exists(blog.ChildId)).ToList();

            if (!incompleteBlogs.Any())
                return;

            string incompleteBlogNames = incompleteBlogs.Select(blog => blog.ChildId).Aggregate((a, b) => a + ", " + b);
            Logger.Verbose("ManagerController:CheckIfDatabasesComplete: {0}", incompleteBlogNames);
            shellService.ShowError(new SerializationException(), Resources.CouldNotLoadLibrary, incompleteBlogNames);

            foreach (IBlog incompleteBlog in incompleteBlogs)
            {
                managerService.BlogFiles.Remove(incompleteBlog);
            }
        }

        private async Task CheckBlogsOnlineStatusAsync()
        {
            if (shellService.Settings.CheckOnlineStatusOnStartup)
            {
                IEnumerable<IBlog> blogs = managerService.BlogFiles;
                await Task.Run(() => ThrottledCheckStatusOfBlogsAsync(blogs));
            }
        }

        private async Task CheckStatusAsync()
        {
            IEnumerable<IBlog> blogs = selectionService.SelectedBlogFiles.ToArray();
            await Task.Run(() => ThrottledCheckStatusOfBlogsAsync(blogs));
        }

        private async Task ThrottledCheckStatusOfBlogsAsync(IEnumerable<IBlog> blogs)
        {
            var semaphoreSlim = new SemaphoreSlim(25);
            IEnumerable<Task> tasks = blogs.Select(async blog => await CheckStatusOfBlogsAsync(semaphoreSlim, blog));
            await Task.WhenAll(tasks);
        }

        private async Task CheckStatusOfBlogsAsync(SemaphoreSlim semaphoreSlim, IBlog blog)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                ICrawler crawler = crawlerFactory.GetCrawler(blog, new CancellationToken(), new PauseToken(),
                    new Progress<DownloadProgress>());
                await crawler.IsBlogOnlineAsync();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private bool CanLoadLibrary() => !crawlerService.IsCrawl;

        private bool CanLoadAllDatbases() => !crawlerService.IsCrawl;

        private bool CanCheckIfDatabasesComplete() => crawlerService.DatabasesLoaded.Task.GetAwaiter().IsCompleted &&
                                                      crawlerService.LibraryLoaded.Task.GetAwaiter().IsCompleted;

        private bool CanEnqueueSelected() => ManagerViewModel.SelectedBlogFile != null && ManagerViewModel.SelectedBlogFile.Online;

        private void EnqueueSelected() => Enqueue(selectionService.SelectedBlogFiles.Where(blog => blog.Online).ToArray());

        private void Enqueue(IEnumerable<IBlog> blogFiles) => QueueManager.AddItems(blogFiles.Select(x => new QueueListItem(x)));

        private bool CanEnqueueAutoDownload() => managerService.BlogFiles.Any();

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
                    managerService
                        .BlogFiles.Where(blog => blog.Online && blog.LastCompleteCrawl != new DateTime(0L, DateTimeKind.Utc))
                        .ToArray());
            }

            if (shellService.Settings.BlogType == shellService.Settings.BlogTypes.ElementAtOrDefault(3))
            {
                Enqueue(
                    managerService
                        .BlogFiles.Where(blog => blog.Online && blog.LastCompleteCrawl == new DateTime(0L, DateTimeKind.Utc))
                        .ToArray());
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

        private bool CanAddBlog() => blogFactory.IsValidTumblrBlogUrl(crawlerService.NewBlogUrl);

        private async Task AddBlog()
        {
            try
            {
                await AddBlogAsync(null);
            }
            catch
            {
            }
        }

        private async Task ImportBlogs()
        {
            try
            {
                var fileBrowser = new OpenFileDialog()
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
                };

                if (fileBrowser.ShowDialog() != DialogResult.OK)
                    return;

                var path = fileBrowser.FileName;

                if (!File.Exists(path))
                {
                    Logger.Warning("ManagerController:ImportBlogs: An attempt was made to import blogs from a file which doesn't exist.");
                    return;
                }

                string fileContent;

                using (var streamReader = new StreamReader(path))
                {
                    fileContent = await streamReader.ReadToEndAsync();
                }

                var blogUris = fileContent.Split().Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x));

                await Task.Run(() => AddBlogBatchedAsync(blogUris));
            }
            catch (Exception ex)
            {
                Logger.Error($"ManagerController:ImportBlogs: {ex}");
            }
        }

        private bool CanRemoveBlog() => ManagerViewModel.SelectedBlogFile != null;

        private void RemoveBlog()
        {
            IBlog[] blogs = selectionService.SelectedBlogFiles.ToArray();

            if (shellService.Settings.DisplayConfirmationDialog)
            {
                string blogNames = string.Join(", ", blogs.Select(blog => blog.Name));
                string message = string.Empty;
                message = string.Format(
                    shellService.Settings.DeleteOnlyIndex ? Resources.DeleteBlogsDialog : Resources.DeleteBlogsAndFilesDialog,
                    blogNames);

                if (!messageService.ShowYesNoQuestion(this, message))
                    return;
            }

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
                if (shellService.Settings.LoadAllDatabases)
                {
                    managerService.RemoveDatabase(managerService.Databases
                                                                .FirstOrDefault(db =>
                                                                    db.Name.Equals(blog.Name) &&
                                                                    db.BlogType.Equals(blog.BlogType)));
                }

                QueueManager.RemoveItems(QueueManager.Items.Where(item => item.Blog.Equals(blog)));
            }
        }

        private bool CanShowFiles() => ManagerViewModel.SelectedBlogFile != null;

        private void ShowFiles()
        {
            foreach (IBlog blog in selectionService.SelectedBlogFiles.ToArray())
            {
                Process.Start("explorer.exe", blog.DownloadLocation());
            }
        }

        private bool CanVisitBlog() => ManagerViewModel.SelectedBlogFile != null;

        private void VisitBlog()
        {
            foreach (IBlog blog in selectionService.SelectedBlogFiles.ToArray())
            {
                Process.Start(blog.Url);
            }
        }

        private void VisitBlogOnTumbex()
        {
            foreach (IBlog blog in selectionService.SelectedBlogFiles.ToArray())
            {
                string tumbexUrl = $"https://www.tumbex.com/{blog.Name}.tumblr/";
                Process.Start(tumbexUrl);
            }
        }

        private void ShowDetailsCommand() => shellService.ShowDetailsView();

        private void CopyUrl()
        {
            List<string> urls = selectionService.SelectedBlogFiles.Select(blog => blog.Url).ToList();
            urls.Sort();
            clipboardService.SetText(string.Join(Environment.NewLine, urls));
        }

        private bool CanCopyUrl() => ManagerViewModel.SelectedBlogFile != null;

        private bool CanCheckStatus() => ManagerViewModel.SelectedBlogFile != null;

        private async Task AddBlogAsync(string blogUrl)
        {
            if (string.IsNullOrEmpty(blogUrl))
                blogUrl = crawlerService.NewBlogUrl;

            IBlog blog = CheckIfCrawlableBlog(blogUrl);

            blog = await CheckIfBlogIsHiddenTumblrBlogAsync(blog);

            lock (lockObject)
            {
                if (CheckIfBlogAlreadyExists(blog))
                    return;

                SaveBlog(blog);
            }

            blog = settingsService.TransferGlobalSettingsToBlog(blog);
            await UpdateMetaInformationAsync(blog);
        }

        private void SaveBlog(IBlog blog)
        {
            if (blog.Save())
                AddToManager(blog);
        }

        private bool CheckIfBlogAlreadyExists(IBlog blog)
        {
            if (managerService.BlogFiles.Any(blogs => blogs.Name.Equals(blog.Name) && blogs.BlogType.Equals(blog.BlogType)))
            {
                shellService.ShowError(null, Resources.BlogAlreadyExist, blog.Name);
                return true;
            }

            return false;
        }

        private async Task UpdateMetaInformationAsync(IBlog blog)
        {
            ICrawler crawler = crawlerFactory.GetCrawler(blog, new CancellationToken(), new PauseToken(),
                new Progress<DownloadProgress>());

            await crawler.UpdateMetaInformationAsync();
        }

        private IBlog CheckIfCrawlableBlog(string blogUrl)
        {
            return blogFactory.GetBlog(blogUrl, Path.Combine(shellService.Settings.DownloadLocation, "Index"));
        }

        private void AddToManager(IBlog blog)
        {
            QueueOnDispatcher.CheckBeginInvokeOnUI(() => managerService.BlogFiles.Add(blog));
            if (shellService.Settings.LoadAllDatabases)
                managerService.AddDatabase(new Files().Load(blog.ChildId));
        }

        private async Task<IBlog> CheckIfBlogIsHiddenTumblrBlogAsync(IBlog blog)
        {
            if (blog.GetType() == typeof(TumblrBlog) && await tumblrBlogDetector.IsHiddenTumblrBlogAsync(blog.Url))
            {
                RemoveBlog(new[] { blog });
                blog = TumblrHiddenBlog.Create("https://www.tumblr.com/dashboard/blog/" + blog.Name,
                    Path.Combine(shellService.Settings.DownloadLocation, "Index"));
            }

            return blog;
        }

        private void OnClipboardContentChanged(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsText())
                return;

            // Count each whitespace as new url
            string[] urls = Clipboard.GetText().Split();

            Task.Run(() => AddBlogBatchedAsync(urls));
        }

        private async Task AddBlogBatchedAsync(IEnumerable<string> urls)
        {
            var semaphoreSlim = new SemaphoreSlim(25);

            await addBlogSemaphoreSlim.WaitAsync();
            try
            {
                IEnumerable<Task> tasks = urls.Select(async url => await AddBlogsAsync(semaphoreSlim, url));
                await Task.WhenAll(tasks);
            }
            finally
            {
                addBlogSemaphoreSlim.Release();
            }
        }

        private async Task AddBlogsAsync(SemaphoreSlim semaphoreSlim, string url)
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                await AddBlogAsync(url);
            }
            catch
            {
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private void ListenClipboard()
        {
            if (shellService.Settings.CheckClipboard)
                shellService.ClipboardMonitor.OnClipboardContentChanged += OnClipboardContentChanged;
            else
                shellService.ClipboardMonitor.OnClipboardContentChanged -= OnClipboardContentChanged;
        }

        private void CrawlerServicePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(crawlerService.NewBlogUrl))
                addBlogCommand.RaiseCanExecuteChanged();
        }

        private void ManagerViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ManagerViewModel.SelectedBlogFile))
                UpdateCommands();
        }

        private void UpdateCommands()
        {
            enqueueSelectedCommand.RaiseCanExecuteChanged();
            removeBlogCommand.RaiseCanExecuteChanged();
            showFilesCommand.RaiseCanExecuteChanged();
        }

        public void RestoreColumn() => ManagerViewModel.DataGridColumnRestore();
    }
}
