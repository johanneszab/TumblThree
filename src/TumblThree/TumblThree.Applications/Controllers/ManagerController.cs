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
using System.Collections.Concurrent;
using TumblThree.Applications.Downloader;

namespace TumblThree.Applications.Controllers
{
    [Export]
    internal class ManagerController
    {
        private readonly IShellService shellService;
        private readonly ISelectionService selectionService;
        private readonly IManagerService managerService;
        private readonly ICrawlerService crawlerService;
        private readonly Lazy<ManagerViewModel> managerViewModel;
        private readonly ObservableCollection<Blog> blogFiles;
        private readonly DelegateCommand addBlogCommand;
        private readonly DelegateCommand removeBlogCommand;
        private readonly DelegateCommand showFilesCommand;
        private readonly DelegateCommand visitBlogCommand;
        private readonly DelegateCommand enqueueSelectedCommand;
        private readonly DelegateCommand listenClipboardCommand;
        private readonly DelegateCommand autoDownloadCommand;
        private readonly DelegateCommand showDetailsCommand;

        private readonly object lockObject = new object();

        public delegate void BlogManagerFinishedLoadingHandler(object sender, EventArgs e);
        public event BlogManagerFinishedLoadingHandler BlogManagerFinishedLoading;

        [ImportingConstructor]
        public ManagerController(IShellService shellService, ISelectionService selectionService, ICrawlerService crawlerService,
            IManagerService managerService, Lazy<ManagerViewModel> managerViewModel)
        {
            this.shellService = shellService;
            this.selectionService = selectionService;
            this.crawlerService = crawlerService;
            this.managerService = managerService;
            this.managerViewModel = managerViewModel;
            this.blogFiles = new ObservableCollection<Blog>();
            this.addBlogCommand = new DelegateCommand(AddBlog, CanAddBlog);
            this.removeBlogCommand = new DelegateCommand(RemoveBlog, CanRemoveBlog);
            this.showFilesCommand = new DelegateCommand(ShowFiles, CanShowFiles);
            this.visitBlogCommand = new DelegateCommand(VisitBlog, CanVisitBlog);
            this.enqueueSelectedCommand = new DelegateCommand(EnqueueSelected, CanEnqueueSelected);
            this.listenClipboardCommand = new DelegateCommand(ListenClipboard);
            this.autoDownloadCommand = new DelegateCommand(EnqueueAutoDownload, CanEnqueueAutoDownload);
            this.showDetailsCommand = new DelegateCommand(ShowDetailsCommand);
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


        public void Shutdown()
        {
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

        private async Task LoadLibrary()
        {
            Logger.Verbose("ManagerController.LoadLibrary:Start");
            managerService.BlogFiles.Clear();
            var path = Path.Combine(shellService.Settings.DownloadLocation, "Index");

            try
            {
                if (Directory.Exists(path))
                {
                    {
                        var files = await GetFilesAsync(path);
                        foreach (var file in files)
                        {
                            managerService.BlogFiles.Add(file);
                        }

                        if (BlogManagerFinishedLoading != null)
                        {
                            BlogManagerFinishedLoading(this, EventArgs.Empty);
                        }

                        if (shellService.Settings.CheckOnlineStatusAtStartup)
                        {
                            foreach (var file in files)
                            {
                                Downloader.Downloader downloader = new Downloader.Downloader(shellService, file);
                                await downloader.IsBlogOnline();
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
            return Task<IReadOnlyList<Blog>>.Factory.StartNew(() =>
            {
                return GetFilesCore(directory);
            },
            TaskCreationOptions.LongRunning);
        }

        private IReadOnlyList<Blog> GetFilesCore(string directory)
        {
            Logger.Verbose("ManagerController:GetFilesCore Start");

            List<Blog> blogs = new List<Blog>();

            var supportedFileTypes = Enum.GetNames(typeof(BlogTypes)).ToArray();

            foreach (var filename in Directory.GetFiles(directory, "*").Where(
                fileName => supportedFileTypes.Any(fileName.Contains) && 
                !fileName.Contains("_files")))
            {
                try
                {
                    using (FileStream stream = new FileStream(filename,
                        FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        string json = File.ReadAllText(filename);
                        TumblrBlog blog = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<TumblrBlog>(json);
                        blog.ChildId = Path.Combine(directory, blog.Name + "_files.tumblr");
                        blog.Location = directory;
                        blog.Update();
                        blogs.Add(blog);
                    }
                }
                catch (SerializationException ex)
                {
                    ex.Data["Filename"] = filename;
                    throw;
                }
            }
            Logger.Verbose("ManagerController.GetFilesCore End");

            return blogs;
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
                Enqueue(managerService.BlogFiles.Where(blog => blog.Online).ToArray());
            if (shellService.Settings.BlogType == shellService.Settings.BlogTypes.ElementAtOrDefault(2))
                Enqueue(managerService.BlogFiles.Where(blog => blog.Online && blog.LastCompleteCrawl != System.DateTime.MinValue).ToArray());
            if (shellService.Settings.BlogType == shellService.Settings.BlogTypes.ElementAtOrDefault(3))
                Enqueue(managerService.BlogFiles.Where(blog => blog.Online && blog.LastCompleteCrawl == System.DateTime.MinValue).ToArray());

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
            var blogs = selectionService.SelectedBlogFiles.ToArray();

            foreach (var blog in blogs)
            {
                if (!shellService.Settings.DeleteOnlyIndex)
                {
                    try
                    {
                        string blogPath = Directory.GetParent(blog.Location).FullName;
                        Directory.Delete(Path.Combine(blogPath, blog.Name), true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("ManagerController:RemoveBlog: {0}", ex);
                        shellService.ShowError(ex, Resources.CouldNotRemoveBlog, blog.Name);
                        return;
                    }
                }

                var indexFile = Path.Combine(blog.Location, blog.Name) + "." + blog.BlogType;
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

        public async Task AddBlogAsync(string blogUrl)
        {
            if (String.IsNullOrEmpty(blogUrl))
            {
                blogUrl = crawlerService.NewBlogUrl;
            }

            // FIXME: 
            TumblrBlog blog = new TumblrBlog(blogUrl, Path.Combine(shellService.Settings.DownloadLocation, "Index"), BlogTypes.tumblr);
            TransferGlobalSettingsToBlog(blog);
            TumblrDownloader downloader = new TumblrDownloader(shellService, crawlerService, blog);
            await downloader.IsBlogOnline();
            await downloader.UpdateMetaInformation();

            lock (lockObject)
            {
                if (managerService.BlogFiles.Select(blogs => blogs.Name).ToList().Contains(blog.Name))
                {
                    shellService.ShowError(null, Resources.BlogAlreadyExist, blog.Name);
                    return;
                }

                if (blog.Save())
                {
                    QueueOnDispatcher.CheckBeginInvokeOnUI((Action)(() => managerService.BlogFiles.Add(blog)));
                }
            }
        }

        private void TransferGlobalSettingsToBlog(TumblrBlog blog)
        {
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
            blog.CheckDirectoryForFiles = shellService.Settings.CheckDirectoryForFiles;
            blog.DownloadUrlList = shellService.Settings.DownloadUrlList;
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
