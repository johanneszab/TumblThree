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
using System.Web.Script.Serialization;
using System.Web;
using System.Net;
using System.Text;

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
        //private readonly DelegateCommand showBlogPropertiesCommand;
        private readonly List<Task> runningTasks;
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
            this.runningTasks = new List<Task>();
            this.locked = false;
        }

        private ManagerViewModel ManagerViewModel { get { return managerViewModel.Value; } }

        public ManagerSettings ManagerSettings { get; set; }

        public QueueManager QueueManager { get; set; }

        public void Initialize()
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

            ManagerViewModel.PropertyChanged += ManagerViewModelPropertyChanged;
        
            shellService.ContentView = ManagerViewModel.View;

            LoadLibrary();

            if (shellService.Settings.CheckClipboard)
                shellService.ClipboardMonitor.OnClipboardContentChanged += OnClipboardContentChanged;
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
        }

        private async void LoadLibrary()
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
                                file.Online = await IsBlogOnline(file.Name);
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

            foreach (var filename in Directory.GetFiles(directory))
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

                    Monitor.Enter(selectionService.ActiveItems);
                    selectionService.AddActiveItems(blogToCrawlNext);
                    Monitor.Exit(selectionService.ActiveItems);


                    Monitor.Exit(QueueManager.Items);

                    if (blogToCrawlNext.Blog is TumblrBlog) {

                        var blog = (TumblrBlog) blogToCrawlNext.Blog;
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

            int downloadedPhotos = (int)blog.DownloadedPhotos;
            int downloadedVideos = (int)blog.DownloadedVideos;
            int downloadedAudios = (int)blog.DownloadedAudios;
            int downloadedTexts = (int)blog.DownloadedTexts;
            int downloadedQuotes = (int)blog.DownloadedQuotes;
            int downloadedLinks = (int)blog.DownloadedLinks;
            int downloadedConversations = (int)blog.DownloadedConversations;

            blog.TotalCount = newImageCount;

            newImageUrls.RemoveAll(item => blog.Links.Contains(item.Item1));

            var indexPath = Path.Combine(shellService.Settings.DownloadLocation, "Index");
            var blogPath = shellService.Settings.DownloadLocation;

            var parallel = Parallel.ForEach(
                newImageUrls,
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

                        //FIXME: Create more generic method to remove WET code.
                        switch (currentImageUrl.Item2)
                        {
                            case "Photo":
                                fileName = currentImageUrl.Item1.Split('/').Last();
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), fileName);

                                if (Download(blog, fileLocation, currentImageUrl.Item1, progress))
                                {
                                    blog.Links.Add(currentImageUrl.Item1);
                                    blog.DownloadedImages = (uint)blog.Links.Count();
                                    blog.Progress = (uint)((double)blog.DownloadedImages / (double)blog.TotalCount * 100);
                                }

                                if (shellService.Settings.EnablePreview)
                                    blog.LastDownloadedPhoto = Path.GetFullPath(fileLocation);
                                Interlocked.Increment(ref downloadedPhotos);
                                blog.DownloadedPhotos = (uint)downloadedPhotos;
                                break;
                            case "Video":
                                fileName = currentImageUrl.Item1.Split('/').Last();
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), fileName);

                                if (Download(blog, fileLocation, currentImageUrl.Item1, progress))
                                {
                                    blog.Links.Add(currentImageUrl.Item1);
                                    blog.DownloadedImages = (uint)blog.Links.Count();
                                    blog.Progress = (uint)((double)blog.DownloadedImages / (double)blog.TotalCount * 100);
                                }

                                if (shellService.Settings.EnablePreview)
                                    blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);
                                Interlocked.Increment(ref downloadedVideos);
                                blog.DownloadedVideos = (uint)downloadedVideos;
                                break;
                            case "Audio":
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), currentImageUrl.Item3 + ".mp3");

                                if (Download(blog, fileLocation, currentImageUrl.Item1, progress))
                                {
                                    blog.Links.Add(currentImageUrl.Item1);
                                    blog.DownloadedImages = (uint)blog.Links.Count();
                                    blog.Progress = (uint)((double)blog.DownloadedImages / (double)blog.TotalCount * 100);
                                }
                                Interlocked.Increment(ref downloadedAudios);
                                blog.DownloadedAudios = (uint)downloadedAudios;
                                break;
                            case "Text":
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameTexts));

                                if (Download(blog, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress))
                                {
                                    blog.Links.Add(currentImageUrl.Item3);
                                    blog.DownloadedImages = (uint)blog.Links.Count();
                                    blog.Progress = (uint)((double)blog.DownloadedImages / (double)blog.TotalCount * 100);
                                }
                                Interlocked.Increment(ref downloadedTexts);
                                blog.DownloadedTexts = (uint)downloadedTexts;
                                break;
                            case "Quote":
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameQuotes));

                                if (Download(blog, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress))
                                {
                                    blog.Links.Add(currentImageUrl.Item3);
                                    blog.DownloadedImages = (uint)blog.Links.Count();
                                    blog.Progress = (uint)((double)blog.DownloadedImages / (double)blog.TotalCount * 100);
                                }
                                Interlocked.Increment(ref downloadedQuotes);
                                blog.DownloadedQuotes = (uint)downloadedQuotes;
                                break;
                            case "Link":
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameLinks));

                                if (Download(blog, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress))
                                {
                                    blog.Links.Add(currentImageUrl.Item3);
                                    blog.DownloadedImages = (uint)blog.Links.Count();
                                    blog.Progress = (uint)((double)blog.DownloadedImages / (double)blog.TotalCount * 100);
                                }
                                Interlocked.Increment(ref downloadedLinks);
                                blog.DownloadedLinks = (uint)downloadedLinks;
                                break;
                            case "Conversation":
                                fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameConversations));

                                if (Download(blog, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress))
                                {
                                    blog.Links.Add(currentImageUrl.Item3);
                                    blog.DownloadedImages = (uint)blog.Links.Count();
                                    blog.Progress = (uint)((double)blog.DownloadedImages / (double)blog.TotalCount * 100);
                                }
                                Interlocked.Increment(ref downloadedConversations);
                                blog.DownloadedConversations = (uint)downloadedConversations;
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

            var newProgress = new DataModels.DownloadProgress();
            newProgress.Progress = "";
            progress.Report(newProgress);

            return blog;
        }

        private bool CanEnqueueSelected()
        {
            return ManagerViewModel.SelectedBlogFile != null;
        }

        private void EnqueueSelected()
        {
            Enqueue(selectionService.SelectedBlogFiles.ToArray());
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
                Enqueue(selectionService.BlogFiles.ToArray());
            if (shellService.Settings.BlogType == shellService.Settings.BlogTypes.ElementAtOrDefault(2))
                Enqueue(selectionService.BlogFiles.Where(blog => blog.LastCompleteCrawl != System.DateTime.MinValue).ToArray());
            if (shellService.Settings.BlogType == shellService.Settings.BlogTypes.ElementAtOrDefault(3))
                Enqueue(selectionService.BlogFiles.Where(blog => blog.LastCompleteCrawl == System.DateTime.MinValue).ToArray());

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

        private Task<bool> IsBlogOnline(string name)
        {
            return Task<bool>.Factory.StartNew(() =>
            {
                string url = GetApiUrl(name, 1);
                string authHeader = shellService.OAuthManager.GenerateauthHeader(url, "GET");

                var blogDoc = RequestData(url, authHeader);
                if (blogDoc != null)
                    return true;
                else
                    return false;
            },
            TaskCreationOptions.LongRunning);
        }

        private string GetApiUrl(string account, int count, int start = 0)
        {
            /// <summary>
            /// construct the tumblr api post url of a blog.
            /// <para>the blog for the url</para>
            /// </summary>
            /// <returns>A string containing the api url of the blog.</returns>
            string name = account;
            if (!account.Contains("."))
                name = name += ".tumblr.com";
            string baseUrl = "https://api.tumblr.com/v2/blog/" + name + "/posts";

            var parameters = new Dictionary<string, string>
            {
              { "api_key", shellService.Settings.ApiKey },
              { "reblog_info", "true" },
              { "limit", count.ToString() }
            };
            if (start > 0)
                parameters["offset"] = start.ToString();
            return baseUrl + "?" + UrlEncode(parameters);
        }

        private Datamodels.Json.TumblrJson RequestData(string url, string authHeaders)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.ServicePoint.Expect100Continue = false;
                request.ContentType = "x-www-from-urlencoded";

                if (authHeaders != null)
                {
                    // add OAuth header
                    request.Headers["Authorization"] = authHeaders;
                }

                var jsserializer = new JavaScriptSerializer();
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (ThrottledStream stream = new ThrottledStream(response.GetResponseStream(), (shellService.Settings.Bandwidth / shellService.Settings.ParallelImages) * 1024))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            DataModels.Json.TumblrJson data = jsserializer.Deserialize<DataModels.Json.TumblrJson>(reader.ReadToEnd());
                            if (data.meta.status == 200)
                                return data;
                            return null;
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
            return ("http://" + ExtractBlogname(url) + ".tumblr.com/");
        }

        public async Task AddBlogAsync(string blogUrl)
        {
            if (String.IsNullOrEmpty(blogUrl))
            {
                blogUrl = crawlerService.NewBlogUrl;
            }

            string blogName = ExtractBlogname(blogUrl);
            TumblrBlog blog = new TumblrBlog(ExtractUrl(blogUrl));

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

            blog.Name = blogName;
            blog.DownloadAudio = shellService.Settings.DownloadAudios;
            blog.DownloadPhoto = shellService.Settings.DownloadImages;
            blog.DownloadVideo = shellService.Settings.DownloadVideos;
            blog.DownloadText = shellService.Settings.DownloadTexts;
            blog.DownloadQuote = shellService.Settings.DownloadQuotes;
            blog.DownloadConversation = shellService.Settings.DownloadConversations;
            blog.DownloadLink = shellService.Settings.DownloadLinks;
            blog.SkipGif = shellService.Settings.SkipGif;

            blog = await GetMetaInformation(blog);

            blog.Online = await IsBlogOnline(blog.Name);

            SaveBlog(blog);
        }

        public bool SaveBlog(Blog blog)
        {
            if (blog == null)
                return false;

            var indexPath = Path.Combine(shellService.Settings.DownloadLocation, "Index");
            var blogPath = shellService.Settings.DownloadLocation;

            try
            {
                CreateDataFolder("Index", blogPath);
                CreateDataFolder(blog.Name, blogPath);
                using (FileStream stream = new FileStream(Path.Combine(indexPath, blog.Name + ".tumblr"),
                    FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, blog);
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
                string url = GetApiUrl(blog.Name, 1);
                string authHeader = shellService.OAuthManager.GenerateauthHeader(url, "GET");

                var blogDoc = RequestData(url, authHeader);

                if (blogDoc != null)
                {
                    blog.Title = blogDoc.response.blog.title;
                    blog.Description = blogDoc.response.blog.description;
                    blog.TotalCount = (uint) blogDoc.response.blog.total_posts;
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
            uint totalImages;
            int photos = 0;
            int videos = 0;
            int audio = 0;
            int text = 0;
            int conversation = 0;
            int quotes = 0;
            int link = 0;
            List<Tuple<string, string, string>> images = new List<Tuple<string, string, string>>();

            string url = GetApiUrl(blog.Name, 1);
            string authHeader = shellService.OAuthManager.GenerateauthHeader(url, "GET");

            var blogDoc = RequestData(url, authHeader);
            totalPosts = blogDoc.response.blog.total_posts;

            // Generate URL list of Images
            // the api v2 shows 20 posts at max, determine the number of pages to crawl
            int totalPages = (totalPosts / 20) + 1;

            Parallel.For(0, totalPages,
                        new ParallelOptions { MaxDegreeOfParallelism = (shellService.Settings.ParallelImages / selectionService.ActiveItems.Count) },
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
                                    DataModels.Json.TumblrJson document = null;

                                    // get 20 posts per crawl/page
                                    url = GetApiUrl(blog.Name, 20, i * 20);
                                    authHeader = shellService.OAuthManager.GenerateauthHeader(url, "GET");

                                    document = RequestData(url, authHeader);

                                    //FIXME: Create Generic Method to reduce WET code
                                    Interlocked.Add(ref photos, document.response.posts.Where(posts => posts.type.Equals("photo")).Count());
                                    Interlocked.Add(ref videos, document.response.posts.Where(posts => posts.type.Equals("video")).Count());
                                    Interlocked.Add(ref audio, document.response.posts.Where(posts => posts.type.Equals("audio")).Count());
                                    Interlocked.Add(ref text, document.response.posts.Where(posts => posts.type.Equals("text")).Count());
                                    Interlocked.Add(ref conversation, document.response.posts.Where(posts => posts.type.Equals("chat")).Count());
                                    Interlocked.Add(ref quotes, document.response.posts.Where(posts => posts.type.Equals("quotes")).Count());
                                    Interlocked.Add(ref link, document.response.posts.Where(posts => posts.type.Equals("link")).Count());

                                    if (blog.DownloadPhoto == true)
                                    {
                                        foreach (Datamodels.Json.Post post in document.response.posts.Where(posts => posts.type.Equals("photo")))
                                        {
                                            foreach (DataModels.Json.Photo photo in post.photos)
                                            {
                                                var imageUrl = photo.alt_sizes.ElementAt(shellService.Settings.ImageSizes.IndexOf(shellService.Settings.ImageSize.ToString())).url;
                                                if (blog.SkipGif == true && imageUrl.EndsWith(".gif"))
                                                    continue;
                                                Monitor.Enter(images);
                                                images.Add(Tuple.Create(imageUrl, "Photo", post.id.ToString()));
                                                Monitor.Exit(images);
                                            }
                                        }
                                    }
                                    if (blog.DownloadVideo == true)
                                    {
                                        foreach (DataModels.Json.Post post in document.response.posts.Where(posts => posts.type.Equals("video")))
                                        {
                                            if (shellService.Settings.VideoSize == 1080)
                                            {
                                                Monitor.Enter(images);
                                                images.Add(Tuple.Create(post.video_url, "Video", post.id.ToString()));
                                                Monitor.Exit(images);
                                            }
                                            if (shellService.Settings.VideoSize == 480)
                                            {
                                                Monitor.Enter(images);
                                                images.Add(Tuple.Create(post.video_url.Insert(post.video_url.LastIndexOf("."), "_480"), "Video", post.id.ToString()));
                                                Monitor.Exit(images);
                                            }
                                        }
                                    }
                                    if (blog.DownloadAudio == true)
                                    {
                                        foreach (DataModels.Json.Post post in document.response.posts.Where(posts => posts.type.Equals("audio")))
                                        {
                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(post.audio_source_url, "Audio", post.id.ToString()));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.DownloadText == true)
                                    {
                                        foreach (DataModels.Json.Post post in document.response.posts.Where(posts => posts.type.Equals("text")))
                                        {
                                            string textBody = "Post ID: " + post.id.ToString() + ", Date: " + post.date + Environment.NewLine + "Title: " + post.title + Environment.NewLine + post.body + Environment.NewLine;

                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Text", post.id.ToString()));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.DownloadQuote == true)
                                    {
                                        foreach (DataModels.Json.Post post in document.response.posts.Where(posts => posts.type.Equals("quote")))
                                        {
                                            string textBody = "Post ID: " + post.id.ToString() + ", Date: " + post.date + Environment.NewLine + "Quote: " + post.text + Environment.NewLine + post.source + Environment.NewLine;

                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Quote", post.id.ToString()));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.DownloadLink == true)
                                    {
                                        foreach (DataModels.Json.Post post in document.response.posts.Where(posts => posts.type.Equals("link")))
                                        {
                                            string textBody = "Post ID: " + post.id.ToString() + ", Date: " + post.date + Environment.NewLine + "Link: " + post.title + Environment.NewLine + post.url + Environment.NewLine + post.description + Environment.NewLine;

                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Link", post.id.ToString()));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.DownloadConversation == true)
                                    {
                                        foreach (DataModels.Json.Post post in document.response.posts.Where(posts => posts.type.Equals("chat")))
                                        {
                                            string textBody = "Post ID: " + post.id.ToString() + ", Date: " + post.date + Environment.NewLine + "Conversation: " + post.body + Environment.NewLine;

                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Conversation", post.id.ToString()));
                                            Monitor.Exit(images);
                                        }
                                    }
                                }
                                // crawling only for tagged images
                                else
                                {
                                    List<string> tags = blog.Tags.Split(',').Select(x => x.Trim()).ToList();

                                    DataModels.Json.TumblrJson document = null;

                                    // get 20 posts per crawl/page
                                    url = GetApiUrl(blog.Name, 20, i * 20);
                                    authHeader = shellService.OAuthManager.GenerateauthHeader(url, "GET");

                                    document = RequestData(url, authHeader);

                                    if (blog.DownloadPhoto == true)
                                    {
                                        foreach (Datamodels.Json.Post post in document.response.posts.Where(posts => posts.tags.Any(tag => tags.Equals(tag)) && posts.type.Equals("photo")))
                                        {
                                            foreach (DataModels.Json.Photo photo in post.photos ?? new List<Datamodels.Json.Photo>())
                                            {
                                                var imageUrl = photo.alt_sizes.ElementAt(shellService.Settings.ImageSizes.IndexOf(shellService.Settings.ImageSize.ToString())).url;
                                                if (blog.SkipGif == true && imageUrl.EndsWith(".gif"))
                                                    continue;
                                                Monitor.Enter(images);
                                                images.Add(Tuple.Create(imageUrl, "Photo", post.id.ToString()));
                                                Monitor.Exit(images);
                                            }
                                        }
                                    }
                                    if (blog.DownloadVideo == true)
                                    {
                                        foreach (DataModels.Json.Post post in document.response.posts.Where(posts => posts.tags.Any(tag => tags.Equals(tag)) && posts.type.Equals("video")))
                                        {
                                            if (shellService.Settings.VideoSize == 1080)
                                            {
                                                Monitor.Enter(images);
                                                images.Add(Tuple.Create(post.video_url, "Video", post.id.ToString()));
                                                Monitor.Exit(images);
                                            }
                                            if (shellService.Settings.VideoSize == 480)
                                            {
                                                Monitor.Enter(images);
                                                images.Add(Tuple.Create(post.video_url.Insert(post.video_url.LastIndexOf("."), "_480"), "Video", post.id.ToString()));
                                                Monitor.Exit(images);
                                            }
                                        }
                                    }
                                    if (blog.DownloadAudio == true)
                                    {
                                        foreach (DataModels.Json.Post post in document.response.posts.Where(posts => posts.tags.Any(tag => tags.Equals(tag)) && posts.type.Equals("audio")))
                                        {
                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(post.audio_source_url, "Audio", post.id.ToString()));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.DownloadText == true)
                                    {
                                        foreach (DataModels.Json.Post post in document.response.posts.Where(posts => posts.tags.Any(tag => tags.Equals(tag)) && posts.type.Equals("text")))
                                        {
                                            string textBody = "Post ID: " + post.id.ToString() + ", Date: " + post.date + Environment.NewLine + "Title: " + post.title + Environment.NewLine + post.body + Environment.NewLine;

                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Text", post.id.ToString()));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.DownloadQuote == true)
                                    {
                                        foreach (DataModels.Json.Post post in document.response.posts.Where(posts => posts.tags.Any(tag => tags.Equals(tag)) && posts.type.Equals("quote")))
                                        {
                                            string textBody = "Post ID: " + post.id.ToString() + ", Date: " + post.date + Environment.NewLine + "Quote: " + post.text + Environment.NewLine + post.source + Environment.NewLine;

                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Quote", post.id.ToString()));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.DownloadLink == true)
                                    {
                                        foreach (DataModels.Json.Post post in document.response.posts.Where(posts => posts.tags.Any(tag => tags.Equals(tag)) && posts.type.Equals("link")))
                                        {
                                            string textBody = "Post ID: " + post.id.ToString() + ", Date: " + post.date + Environment.NewLine + "Link: " + post.title + Environment.NewLine + post.url + Environment.NewLine + post.description + Environment.NewLine;

                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Link", post.id.ToString()));
                                            Monitor.Exit(images);
                                        }
                                    }
                                    if (blog.DownloadConversation == true)
                                    {
                                        foreach (DataModels.Json.Post post in document.response.posts.Where(posts => posts.tags.Any(tag => tags.Equals(tag)) && posts.type.Equals("chat")))
                                        {
                                            string textBody = "Post ID: " + post.id.ToString() + ", Date: " + post.date + Environment.NewLine + "Conversation: " + post.body + Environment.NewLine;

                                            Monitor.Enter(images);
                                            images.Add(Tuple.Create(textBody, "Conversation", post.id.ToString()));
                                            Monitor.Exit(images);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Data);
                            }

                            numberOfPostsCrawled += 20;
                            var newProgress = new DataModels.DownloadProgress();
                            newProgress.Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressGetUrl, numberOfPostsCrawled, totalPosts);
                            progress.Report(newProgress);
                        }
                );

            images = images.Distinct().ToList();

            blog.Posts = (uint)totalPosts;
            blog.Photos = (uint)photos;
            blog.Videos = (uint)videos;
            blog.Audios = (uint)audio;
            blog.Texts = (uint)text;
            blog.Conversations = (uint)conversation;
            blog.Quotes = (uint)quotes;
            blog.NumberOfLinks = (uint)link;

            totalImages = (uint)images.Count;
            return Tuple.Create(totalImages, images);
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

        private bool Download(TumblrBlog blog, string fileLocation, string url, IProgress<DataModels.DownloadProgress> progress)
        {
            Monitor.Enter(blog);
            if (blog.Links.Contains(url))
            {
                Monitor.Exit(blog);
                return false;
            }
            else
            {
                Monitor.Exit(blog);
                try
                {
                    var newProgress = new DataModels.DownloadProgress();
                    newProgress.Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressDownloadImage, url.Split('/').Last()); ;
                    progress.Report(newProgress);

                    using (var stream = ThrottledStream.ReadFromURLIntoStream(url, (shellService.Settings.Bandwidth / shellService.Settings.ParallelImages), shellService.Settings.TimeOut))
                        ThrottledStream.SaveStreamToDisk(stream, fileLocation);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private bool Download(TumblrBlog blog, string fileLocation, string postId, string text, IProgress<DataModels.DownloadProgress> progress)
        {
            Monitor.Enter(blog);
            if (blog.Links.Contains(postId))
            {
                Monitor.Exit(blog);
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
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    Monitor.Exit(blog);
                }
            }
        }

        private void OnClipboardContentChanged(object sender, EventArgs e)
        {
            // Catches if Clipboard cannot be accessed.
            try
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
            catch
            {
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
