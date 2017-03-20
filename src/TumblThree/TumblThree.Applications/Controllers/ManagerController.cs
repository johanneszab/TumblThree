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
        private readonly IEnvironmentService environmentService;
        private readonly ISelectionService selectionService;
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
        public ManagerController(IShellService shellService, IEnvironmentService environmentService, ISelectionService selectionService, ICrawlerService crawlerService,
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
                            CommonDownloader downloader = new CommonDownloader(shellService);
                            foreach (var file in files)
                            {
                                file.Online = await downloader.IsBlogOnline(file.Url);
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
            Logger.Verbose("ManagerController:GetFilesAsync Start");

            List<Blog> blogs = new List<Blog>();

            foreach (var filename in Directory.GetFiles(directory, "*.tumblr").Where(name => !name.EndsWith("_files.tumblr")))
            {
                try
                {
                    using (FileStream stream = new FileStream(filename,
                        FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var reader = new BinaryReader(stream, new ASCIIEncoding()))
                        {
                            byte[] buffer = new byte[18];
                            stream.Seek(0x17, 0);
                            buffer = reader.ReadBytes(17);
                            // "TumblThree.Domain" in hex
                            byte[] tumblThreeSignature = new byte[] { 0x54, 0x75, 0x6D, 0x62, 0x6C, 0x54, 0x68, 0x72, 0x65, 0x65, 0x2E, 0x44, 0x6F, 0x6D, 0x61, 0x69, 0x6E };
                            if (buffer.SequenceEqual(tumblThreeSignature))
                            {
                                stream.Seek(0, 0);
                                IFormatter formatter = new BinaryFormatter();
                                TumblrBlog blog = (TumblrBlog)formatter.Deserialize(stream);
                                blog.ChildId = Path.Combine(directory, blog.Name + "_files.tumblr");
                                blog.Location = directory;
                                blog.Update();
                                blogs.Add(blog);
                            }
                            else
                            {
                                string json = File.ReadAllText(filename);
                                TumblrBlog blog = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<TumblrBlog>(json);
                                blog.ChildId = Path.Combine(directory, blog.Name + "_files.tumblr");
                                blog.Location = directory;
                                blog.Update();
                                blogs.Add(blog);
                            }
                        }
                    }
                }
                catch (SerializationException ex)
                {
                    ex.Data["Filename"] = filename;
                    throw;
                }
            }
            Logger.Verbose("ManagerController.UpdateBlogFiles:GetFilesAsync End");

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
                    if (blog is TumblrBlog)
                    {
                        var tumblrFiles = Path.Combine(indexPath, blog.Name) + "_files.tumblr";
                        File.Delete(tumblrFiles);
                    }
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


        public async Task AddBlogAsync(string blogUrl)
        {
            if (String.IsNullOrEmpty(blogUrl))
            {
                blogUrl = crawlerService.NewBlogUrl;
            }

            TumblrBlog blog = new TumblrBlog();
            blog.Url = blogUrl;
            await TransferGlobalSettingsToBlog(blog);

            lock (lockObject)
            {
                if (selectionService.BlogFiles.Select(blogs => blogs.Name).ToList().Contains(blog.Name))
                {
                    shellService.ShowError(null, Resources.BlogAlreadyExist, blog.Name);
                    return;
                }

                if (blog.Save())
                {
                    QueueOnDispatcher.CheckBeginInvokeOnUI((Action)(() => selectionService.BlogFiles.Add(blog)));
                }
            }
        }


        private async Task TransferGlobalSettingsToBlog(TumblrBlog blog)
        {
            blog.Location = Path.Combine(shellService.Settings.DownloadLocation, "Index");
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

            var tumblrDownloader = new TumblrDownloader(shellService, crawlerService, selectionService, blog);
            await tumblrDownloader.SetUpBlog();

            TumblrFiles files = new TumblrFiles();
            files.Location = Path.Combine(shellService.Settings.DownloadLocation, "Index");
            files.Name = blog.Name;

            Directory.CreateDirectory(Path.Combine(shellService.Settings.DownloadLocation, "Index"));
            Directory.CreateDirectory(Path.Combine(shellService.Settings.DownloadLocation, blog.Name));

            files.Save();
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
