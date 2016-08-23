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
using System.Xml.Linq;
using System.Waf.Applications;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.DataModels;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using System.ComponentModel;
using TumblThree.Applications.Properties;
using TumblThree.Domain.Queue;
using System.Windows;
using System.Windows.Threading;
using TumblThree.Applications;
using System.Globalization;

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
        //private readonly DelegateCommand showBlogPropertiesCommand;
        private readonly List<Task> runningTasks;
        private CancellationTokenSource crawlBlogsCancellation;
        private PauseTokenSource crawlBlogsPause;

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
            this.runningTasks = new List<Task>();
        }

        private ManagerViewModel ManagerViewModel { get { return managerViewModel.Value; } }

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
                shellService.ShowError(ex, Resources.CouldNotLoadLibrary);
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
                    TumblrBlog blog = (TumblrBlog)formatter.Deserialize(stream);

                    blogs.Add(blog);
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

                        var progressHandler = new Progress<DownloadProgress>(value =>
                        {
                            blogToCrawlNext.Progress = value.Progress;
                        });
                        var progress = progressHandler as IProgress<DownloadProgress>;

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

        private TumblrBlog CrawlCoreTumblrBlog(TumblrBlog blog, IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            Logger.Verbose("ManagerController.CrawlCoreTumblrBlog:Start");

            var newProgress = new DownloadProgress();

            var tuple = GetImageUrls(blog, progress, ct, pt);
            var newImageCount = tuple.Item1;
            var newImageUrls = tuple.Item2;

            blog.TotalCount = newImageCount;

            var imageUrls = newImageUrls.Except(blog.Links.ToList());

            var indexPath = Path.Combine(shellService.Settings.DownloadLocation, "Index");
            var blogPath = shellService.Settings.DownloadLocation;

            var parallel = Parallel.ForEach(
                imageUrls,
                    new ParallelOptions { MaxDegreeOfParallelism = (shellService.Settings.ParallelImages / shellService.Settings.ParallelBlogs) },
                    (currentImageUrl, state) =>
                    {
                        if (ct.IsCancellationRequested)
                        {
                            state.Break();
                        }
                        if (pt.IsPaused)
                            pt.WaitWhilePausedWithResponseAsyc().Wait();

                        string fileName = currentImageUrl.Split('/').Last();
                        string fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), fileName);

                        if (Download(blog, fileLocation, currentImageUrl))
                        {
                            blog.Links.Add(currentImageUrl);
                            blog.DownloadedImages = (uint) blog.Links.Count();
                            blog.Progress = (uint)((double)blog.DownloadedImages / (double)blog.TotalCount * 100);

                            newProgress = new DownloadProgress();
                            newProgress.Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressDownloadImage, currentImageUrl); ;
                            progress.Report(newProgress);
                        }
                    });

            if (!ct.IsCancellationRequested)
            {
                blog.LastCompleteCrawl = DateTime.Now;
            }
            SaveBlog(blog);

            newProgress = new DownloadProgress();
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
            shellService.ShowQueueView();
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


        private Task<bool> IsBlogOnline(string url)
        {
            return Task<bool>.Factory.StartNew(() =>
            {
                string ApiUrl = GetApiUrl(url);

                XDocument blogDoc = new XDocument();
                try
                {
                    blogDoc = XDocument.Load(ApiUrl.ToString() + "0" + "&num=0");
                    return true;
                }
                catch
                {
                    return false;
                }
            },
            TaskCreationOptions.LongRunning);
        }

        private string GetApiUrl(string url)
        {
            if (url.Last<char>() != '/')
                url += "/api/read?start=";
            else
                url += "api/read?start=";

            return url;
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

            if (selectionService.BlogFiles.Select(blogs => blogs.Name).ToList().Contains(blogName))
            {
                shellService.ShowError(null, Resources.BlogAlreadyExist, blogName);
                return;
            }

            var blogPath = shellService.Settings.DownloadLocation;

            TumblrBlog blog = new TumblrBlog(ExtractUrl(blogUrl));

            blog.Name = blogName;

            if (Application.Current.Dispatcher.CheckAccess())
            {
                selectionService.BlogFiles.Add(blog);
            }
            else
            {
                await Application.Current.Dispatcher.BeginInvoke(
                  DispatcherPriority.Background,
                  new Action(() =>
                  {
                      selectionService.BlogFiles.Add(blog);
                  }));
            }

            //var tuple = GetImageUrls(blog);
            //blog.TotalCount = tuple.Item1;

            blog.TotalCount = GetPostCount(blog);

            blog.Online = await IsBlogOnline(blog.Url);

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

        public XDocument GetBlogDoc(Blog blog, int numPosts, int page)
        {
            XDocument blogDoc = new XDocument();

            try
            {
                blogDoc = XDocument.Load(GetApiUrl(blog.Url) + (numPosts * page) + "&num=" + numPosts);
            }
            catch (Exception ex)
            {
                blog.Online = false;
                Logger.Error("ManagerController:GetBlogDoc: {0}", ex);
                shellService.ShowError(ex, Resources.BlogIsOffline, blog.Name);
            }
            return blogDoc;
        }

        public uint GetPostCount(Blog blog)
        {
            uint count = 0;

            XDocument blogDoc = GetBlogDoc(blog, 0, 0);

            foreach (var type in from data in blogDoc.Descendants("posts") select new { Total = data.Attribute("total").Value })
            {
                count = Convert.ToUInt32(type.Total.ToString());
            }

            return count;
        }

        public Tuple<uint, List<string>> GetImageUrls(TumblrBlog blog, IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            int totalPosts = 0;
            int numberOfPostsCrawled = 0;
            uint totalImages;
            List<string> images = new List<string>();

            var blogDoc = GetBlogDoc(blog, 0, 0);

            foreach (var type in from data in blogDoc.Descendants("posts") select new { Total = data.Attribute("total").Value })
            {
                totalPosts = Convert.ToInt32(type.Total.ToString());
            }

            // Generate URL list of Images
            // the api shows 50 posts at max, determine the number of pages to crawl
            int totalPages = (totalPosts / 50) + 1;

            Parallel.For(0, totalPages,
                        new ParallelOptions { MaxDegreeOfParallelism = (shellService.Settings.ParallelImages / shellService.Settings.ParallelBlogs) },
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
                                    document = XDocument.Load(GetApiUrl(blog.Url) + (i * 50).ToString() + "&num=50");

                                    if (shellService.Settings.DownloadImages == true)
                                    {
                                        foreach (var post in (from data in document.Descendants("post") where data.Attribute("type").Value == "photo" select data))
                                        {
                                            // photoset
                                            if (post.Descendants("photoset").Count() > 0)
                                            {
                                                foreach (var photo in (from photoData in post.Descendants("photoset").Descendants("photo") select photoData))
                                                {
                                                    var imageUrl = String.Concat(photo.Descendants("photo-url").Where(photo_url =>
                                                        photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).Nodes());

                                                    if (shellService.Settings.SkipGif == true && imageUrl.EndsWith(".gif"))
                                                        continue;
                                                    Monitor.Enter(images);
                                                    images.Add(imageUrl);
                                                    Monitor.Exit(images);
                                                }
                                            }
                                            // single image
                                            else
                                            {
                                                var imageUrl = String.Concat(post.Descendants("photo-url").Where(photo_url =>
                                                    photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).Nodes());

                                                if (shellService.Settings.SkipGif == true && imageUrl.EndsWith(".gif"))
                                                    continue;

                                                Monitor.Enter(images);
                                                images.Add(imageUrl);
                                                Monitor.Exit(images);
                                            }
                                        }
                                    }
                                    if (shellService.Settings.DownloadVideos == true)
                                    {
                                        foreach (var post in (from data in document.Descendants("post") where data.Attribute("type").Value == "video" select data))
                                        {
                                            var videoUrl = post.Descendants("video-player").Where(x => x.Value.Contains("<source src=")).Select(result =>
                                            System.Text.RegularExpressions.Regex.Match(
                                                      result.Value, "<source src=\"(.*)\" type=\"video/mp4\">").Groups[1].Value).ToList();

                                            foreach (string video in videoUrl)
                                            {
                                                if (shellService.Settings.VideoSize == 1080)
                                                {
                                                    Monitor.Enter(images);
                                                    images.Add(video.Replace("/480", "") + ".mp4");
                                                    Monitor.Exit(images);
                                                }
                                                else if (shellService.Settings.VideoSize == 480)
                                                {
                                                    Monitor.Enter(images);
                                                    images.Add("http://vt.tumblr.com/" + video.Replace("/480", "").Split('/').Last() + "_480.mp4");
                                                    Monitor.Exit(images);
                                                }
                                            }
                                        }
                                    }
                                }
                                // crawling only for tagged images
                                else
                                {
                                    List<string> tags = blog.Tags.Split(',').Select(x => x.Trim()).ToList();

                                    XDocument document = null;

                                    // get 50 posts per crawl/page
                                    document = XDocument.Load(GetApiUrl(blog.Url) + (i * 50).ToString() + "&num=50");

                                    if (shellService.Settings.DownloadImages == true)
                                    {

                                        foreach (var post in (from data in document.Descendants("post")
                                                              where data.Attribute("type").Value == "photo" &&
                                                              data.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()
                                                              select data))
                                        {
                                            // photoset
                                            if (post.Descendants("photoset").Count() > 0)
                                            {
                                                foreach (var photo in (from data in document.Descendants("post") where data.Attribute("type").Value == "photo" select data))
                                                {
                                                    var imageUrl = String.Concat(photo.Descendants("photo-url").Where(photo_url =>
                                                        photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).Nodes());

                                                    if (shellService.Settings.SkipGif == true && imageUrl.EndsWith(".gif"))
                                                        continue;
                                                    Monitor.Enter(images);
                                                    images.Add(imageUrl);
                                                    Monitor.Exit(images);
                                                }
                                            }
                                            // single image
                                            else
                                            {
                                                var imageUrl = String.Concat(post.Descendants("photo-url").Where(photo_url =>
                                                    photo_url.Attribute("max-width").Value == shellService.Settings.ImageSize.ToString()).Nodes());

                                                if (shellService.Settings.SkipGif == true && imageUrl.EndsWith(".gif"))
                                                    continue;

                                                Monitor.Enter(images);
                                                images.Add(imageUrl);
                                                Monitor.Exit(images);
                                            }
                                        }
                                    }
                                    if (shellService.Settings.DownloadVideos == true)
                                    {
                                        foreach (var post in (from data in document.Descendants("post") where data.Attribute("type").Value == "video" &&
                                                              data.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()
                                                              select data))
                                        {
                                            var videoUrl = post.Descendants("video-player").Where(x => x.Value.Contains("<source src=")).Select(result =>
                                            System.Text.RegularExpressions.Regex.Match(
                                                      result.Value, "<source src=\"(.*)\" type=\"video/mp4\">").Groups[1].Value).ToList();

                                            foreach (string video in videoUrl)
                                            {
                                                if (shellService.Settings.VideoSize == 1080)
                                                {
                                                    Monitor.Enter(images);
                                                    images.Add(video.Replace("/480", "") + ".mp4");
                                                    Monitor.Exit(images);
                                                } else if (shellService.Settings.VideoSize == 480)
                                                {
                                                    Monitor.Enter(images);
                                                    images.Add("http://vt.tumblr.com/" + video.Split('/').Last() + "_480.mp4");
                                                    Monitor.Exit(images);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Data);
                            }

                            numberOfPostsCrawled += 50;
                            var newProgress = new DownloadProgress();
                            newProgress.Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressGetUrl, numberOfPostsCrawled, totalPosts);
                            progress.Report(newProgress);
                        }
                );

            images = images.Distinct().ToList();

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

        private bool Download(TumblrBlog blog, string fileLocation, string url)
        {
            Monitor.Enter(blog);
            if (!blog.Links.Contains(url))
            {
                Monitor.Exit(blog);
                try
                {
                    using (System.Net.WebClient client = new System.Net.WebClient())
                    {
                        client.DownloadFile(url, fileLocation);
                    }
                    return true;
                }
                catch (Exception)
                {
                }
                return false;
            }
            Monitor.Exit(blog);
            return false;
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
