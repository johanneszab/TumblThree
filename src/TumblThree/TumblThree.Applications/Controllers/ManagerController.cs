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
using System.Collections.Concurrent;

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
        private RateLimiter.TimeLimiter timeconstraint;

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

            timeconstraint = RateLimiter.TimeLimiter.GetFromMaxCountByInterval(shellService.Settings.MaxConnections, TimeSpan.FromSeconds(shellService.Settings.ConnectionTimeInterval));

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
                            byte[] tumblThreeSignature = new byte[] { 0x54, 0x75, 0x6D, 0x62, 0x6C, 0x54, 0x68, 0x72, 0x65, 0x65, 0x2E, 0x44, 0x6F, 0x6D, 0x61, 0x69, 0x6E };
                            if (buffer.SequenceEqual(tumblThreeSignature))
                            {
                                stream.Seek(0, 0);
                                IFormatter formatter = new BinaryFormatter();
                                TumblrBlog blog = (TumblrBlog)formatter.Deserialize(stream);
                                blog.ChildId = Path.Combine(directory, blog.Name + "_files.tumblr");
                                UpdateTumblrBlog(blog);
                                blogs.Add(blog);
                            }
                            else
                            {
                                string json = File.ReadAllText(filename);
                                TumblrBlog blog = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<TumblrBlog>(json);
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

        private bool UpdateTumblrBlog(TumblrBlog blog)
        {
            if (blog.Version != "2")
            {
                if (!File.Exists(blog.ChildId))
                {
                    TumblrFiles files = new TumblrFiles();
                    files.Name = blog.Name + "_files";
                    files.Links = blog.Links.Select(item => item?.Split('/').Last()).ToList();
                    blog.Links.Clear();
                    blog.Version = "2";
                    blog.Dirty = true;
                    SaveTumblrFiles(files);
                    files = null;
                }
            }
            return true;
        }

        private TumblrFiles GetTumblrFiles(TumblrBlog blog)
        {
            string filename = blog.ChildId;

            try
            {
                string json = File.ReadAllText(filename);
                System.Web.Script.Serialization.JavaScriptSerializer jsJson = new System.Web.Script.Serialization.JavaScriptSerializer();
                jsJson.MaxJsonLength = 2147483644;
                TumblrFiles files = jsJson.Deserialize<TumblrFiles>(json);
                return files;
            }
            catch (SerializationException ex)
            {
                ex.Data["Filename"] = filename;
                throw;
            }
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
                        var progress = new ProgressThrottler<DataModels.DownloadProgress>(progressHandler);

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

            BlockingCollection<Tuple<string, string, string>> bCollection = new BlockingCollection<Tuple<string, string, string>>();

            var producer = Task.Run(() => GetImageUrls(blog, bCollection, progress, ct, pt));
            var consumer = Task.Run(() => ProcessTumblrBlog(blog, bCollection, progress, ct, pt));
            var blogContent = producer.Result;
            bool limitHit = blogContent.Item2;
            var newImageUrls = blogContent.Item3;

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

            blog.DuplicatePhotos = (uint)duplicatePhotos;
            blog.DuplicateVideos = (uint)duplicateVideos;
            blog.DuplicateAudios = (uint)duplicateAudios;

            bool finishedDownloading = consumer.Result;

            Task.WaitAll(producer, consumer);

            if (!ct.IsCancellationRequested)
            {
                blog.LastCompleteCrawl = DateTime.Now;
                if (finishedDownloading && !limitHit)
                    blog.LastId = blogContent.Item1;
            }

            blog.Dirty = false;
            SaveBlog(blog);

            newProgress = new DataModels.DownloadProgress();
            newProgress.Progress = "";
            progress.Report(newProgress);

            return blog;
        }

        private string resizeTumblrImageUrl(string imageUrl)
        {
            StringBuilder sb = new StringBuilder(imageUrl);
            return sb
                .Replace("_1280", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_540", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_500", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_400", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_250", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_100", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_75sq", "_" + shellService.Settings.ImageSize.ToString())
                .ToString();            
        }

        private void GetTumblrUrlsCore(TumblrBlog blog,
            List<Tuple<string, string, string>> downloadList, // only used for stats calculation
            BlockingCollection<Tuple<string, string, string>> bCollection, // used to store downloads
            XDocument document,
            List<string> tags,
            ref bool limitHit, 
            ref int totalDownloads,
            ref int photos, 
            ref int videos, 
            ref int audios, 
            ref int texts,
            ref int conversations,
            ref int quotes,
            ref int links,
            ref int photoMetas,
            ref int videoMetas,
            ref int audioMetas)
        {
            // FIXME: Remove the WET Code!
            // The same code block is once done with and without tags
            if (blog.DownloadPhoto == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "photo" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
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
                                imageUrl = resizeTumblrImageUrl(imageUrl);
                            }

                            Interlocked.Increment(ref photos);
                            Interlocked.Increment(ref totalDownloads);
                            Monitor.Enter(downloadList);
                            downloadList.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                            Monitor.Exit(downloadList);

                            bCollection.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
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
                            imageUrl = resizeTumblrImageUrl(imageUrl);
                        }

                        Interlocked.Increment(ref photos);
                        Interlocked.Increment(ref totalDownloads);
                        Monitor.Enter(downloadList);
                        downloadList.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                        Monitor.Exit(downloadList);

                        bCollection.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                    }
                }
                // check for inline images
                foreach (var post in document.Descendants("post").Where(posts => true && 
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    if (String.Concat(post.Nodes()).Contains("tumblr_inline"))
                    {
                        Regex regex = new Regex("<img src=\"(.*?)\"");
                        foreach (Match match in regex.Matches(post.Element("regular-body")?.Value))
                        {
                            var imageUrl = match.Groups[1].Value;
                            if (blog.ForceSize)
                            {
                                imageUrl = resizeTumblrImageUrl(imageUrl);
                            }
                            Interlocked.Increment(ref photos);
                            Interlocked.Increment(ref totalDownloads);
                            Monitor.Enter(downloadList);
                            downloadList.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                            Monitor.Exit(downloadList);

                            bCollection.Add(Tuple.Create(imageUrl, "Photo", post.Attribute("id").Value));
                        }
                    }
                }
            }
            if (blog.DownloadVideo == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "video" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    var videoUrl = post.Descendants("video-player").Where(x => x.Value.Contains("<source src=")).Select(result =>
                        System.Text.RegularExpressions.Regex.Match(
                        result.Value, "<source src=\"(.*)\" type=\"video/mp4\">").Groups[1].Value).ToList();

                    foreach (string video in videoUrl)
                    {
                        if (shellService.Settings.VideoSize == 1080)
                        {
                            Interlocked.Increment(ref totalDownloads);
                            Monitor.Enter(downloadList);
                            downloadList.Add(Tuple.Create(video.Replace("/480", "") + ".mp4", "Video", post.Attribute("id").Value));
                            Monitor.Exit(downloadList);

                            bCollection.Add(Tuple.Create(video.Replace("/480", "") + ".mp4", "Video", post.Attribute("id").Value));
                        }
                        else if (shellService.Settings.VideoSize == 480)
                        {
                            Interlocked.Increment(ref totalDownloads);
                            Monitor.Enter(downloadList);
                            downloadList.Add(Tuple.Create("https://vt.tumblr.com/" + video.Replace("/480", "").Split('/').Last() + "_480.mp4", "Video", post.Attribute("id").Value));
                            Monitor.Exit(downloadList);

                            bCollection.Add(Tuple.Create("https://vt.tumblr.com/" + video.Replace("/480", "").Split('/').Last() + "_480.mp4", "Video", post.Attribute("id").Value));
                        }
                    }
                }
            }
            if (blog.DownloadAudio == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "audio" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    var audioUrl = post.Descendants("audio-player").Where(x => x.Value.Contains("src=")).Select(result =>
                        System.Text.RegularExpressions.Regex.Match(
                        result.Value, "src=\"(.*)\" height").Groups[1].Value).ToList();

                    foreach (string audiofile in audioUrl)
                    {
                        Interlocked.Increment(ref totalDownloads);
                        Monitor.Enter(downloadList);
                        downloadList.Add(Tuple.Create(WebUtility.UrlDecode(audiofile), "Audio", post.Attribute("id").Value));
                        Monitor.Exit(downloadList);

                        bCollection.Add(Tuple.Create(WebUtility.UrlDecode(audiofile), "Audio", post.Attribute("id").Value));
                    }

                }
            }
            if (blog.DownloadText == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "regular" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Title, post.Element("regular-title")?.Value) +
                        Environment.NewLine + post.Element("regular-body")?.Value +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                        Environment.NewLine;
                    Interlocked.Increment(ref totalDownloads);
                    bCollection.Add(Tuple.Create(textBody, "Text", post.Attribute("id").Value));
                }
            }
            if (blog.DownloadQuote == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "quote" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.Element("quote-text")?.Value) +
                        Environment.NewLine + post.Element("quote-source")?.Value +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                        Environment.NewLine;
                    Interlocked.Increment(ref totalDownloads);
                    bCollection.Add(Tuple.Create(textBody, "Quote", post.Attribute("id").Value));
                }
            }
            if (blog.DownloadLink == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "link" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
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
                    bCollection.Add(Tuple.Create(textBody, "Link", post.Attribute("id").Value));
                }
            }
            if (blog.DownloadConversation == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "conversation" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Conversation, post.Element("conversation-text")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                        Environment.NewLine;
                    Interlocked.Increment(ref totalDownloads);
                    bCollection.Add(Tuple.Create(textBody, "Conversation", post.Attribute("id").Value));
                }
            }
            if (blog.CreatePhotoMeta == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "photo" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
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
                        bCollection.Add(Tuple.Create(textBody, "PhotoMeta", post.Attribute("id").Value));
                    }
                }
            }
            if (blog.CreateVideoMeta == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "video" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
                {
                    string textBody = string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " + string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.VideoPlayer, post.Element("video-player")?.Value) +
                        Environment.NewLine + string.Format(CultureInfo.CurrentCulture, Resources.Tags, string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                        Environment.NewLine;
                    Interlocked.Increment(ref totalDownloads);
                    Interlocked.Increment(ref videoMetas);
                    bCollection.Add(Tuple.Create(textBody, "VideoMeta", post.Attribute("id").Value));
                }
            }
            if (blog.CreateAudioMeta == true)
            {
                foreach (var post in document.Descendants("post").Where(posts => posts.Attribute("type").Value == "audio" &&
                (!tags.Any()) ? true : posts.Descendants("tag").Where(x => tags.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).Any()))
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
                    bCollection.Add(Tuple.Create(textBody, "AudioMeta", post.Attribute("id").Value));
                }
            }
        }

        public Tuple<ulong, bool, List<Tuple<string, string, string>>> GetImageUrls(TumblrBlog blog, BlockingCollection<Tuple<string, string, string>> bCollection, IProgress<Datamodels.DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            int totalPosts = 0;
            int totalDownloads = 0;
            int numberOfPostsCrawled = 0;
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
            ulong lastId = blog.LastId;
            bool limitHit = false;
            List<string> tags = new List<string>();
            List<Tuple<string, string, string>> downloadList = new List<Tuple<string, string, string>>();

            string postCountUrl = GetApiUrl(blog.Url, 1);

            XDocument blogDoc = null;

            if (blog.ForceRescan)
            {
                blog.ForceRescan = false;
                lastId = 0;
            }

            if (shellService.Settings.LimitConnections)
                blogDoc = timeconstraint.Perform<XDocument>(() => RequestData(postCountUrl)).Result;
            else
                blogDoc = RequestData(postCountUrl);

            totalPosts = Int32.Parse(blogDoc.Element("tumblr").Element("posts").Attribute("total").Value);

            ulong highestId = UInt64.Parse(blogDoc.Element("tumblr").Element("posts").Element("post").Attribute("id").Value);

            // Generate URL list of Images
            // the api shows 50 posts at max, determine the number of pages to crawl
            int totalPages = (totalPosts / 50) + 1;

            var loopState = Parallel.For(0, totalPages,
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
                                if (!String.IsNullOrWhiteSpace(blog.Tags))
                                    tags = blog.Tags.Split(',').Select(x => x.Trim()).ToList();

                                XDocument document = null;

                                // get 50 posts per crawl/page
                                string url = GetApiUrl(blog.Url, 50, i * 50);

                                if (shellService.Settings.LimitConnections)
                                    document = timeconstraint.Perform(() => RequestData(url)).Result;
                                else
                                    document = RequestData(url);

                                if (document == null)
                                    limitHit = true;

                                // Doesn't count photosets
                                //Interlocked.Add(ref photos, document.Descendants("post").Where(post => post.Attribute("type").Value == "photo").Count());
                                Interlocked.Add(ref videos, document.Descendants("post").Where(post => post.Attribute("type").Value == "video").Count());
                                Interlocked.Add(ref audios, document.Descendants("post").Where(post => post.Attribute("type").Value == "audio").Count());
                                Interlocked.Add(ref texts, document.Descendants("post").Where(post => post.Attribute("type").Value == "regular").Count());
                                Interlocked.Add(ref conversations, document.Descendants("post").Where(post => post.Attribute("type").Value == "conversation").Count());
                                Interlocked.Add(ref quotes, document.Descendants("post").Where(post => post.Attribute("type").Value == "quote").Count());
                                Interlocked.Add(ref links, document.Descendants("post").Where(post => post.Attribute("type").Value == "link").Count());

                                ulong highestPostId = UInt64.Parse(document.Element("tumblr").Element("posts").Element("post").Attribute("id").Value);

                                if (highestPostId < lastId)
                                    state.Break();

                                GetTumblrUrlsCore(blog, downloadList, bCollection, document, tags, ref limitHit, ref totalDownloads, ref photos, ref videos,
                                    ref audios, ref texts, ref conversations, ref quotes, ref links, ref photoMetas, ref videoMetas, ref audioMetas);
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

            bCollection.CompleteAdding();

            if (loopState.IsCompleted)
            {
                blog.TotalCount = (uint)totalDownloads;
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
            }

            return Tuple.Create(highestId, limitHit, downloadList);
        }

        private void UpdateProgress(TumblrBlog blog, TumblrFiles files, string fileName, object lockObjectProgress, ref uint duplicates, ref int totalCounter)
        {
            lock (lockObjectProgress)
            {
                files.Links.Add(fileName);
                // the following could be moved out of the lock?
                blog.DownloadedImages = (uint)totalCounter + (uint)duplicates;
                blog.Progress = (uint)(((double)totalCounter + (double)duplicates) / (double)blog.TotalCount * 100);
            }
        }

        private bool ProcessTumblrBlog(TumblrBlog blog, BlockingCollection<Tuple<string, string, string>> bCollection, IProgress<DataModels.DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {

            object lockObjectProgress = new object();
            object lockObjectDownload = new object();
            bool locked = false;

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

            var indexPath = Path.Combine(shellService.Settings.DownloadLocation, "Index");
            var blogPath = shellService.Settings.DownloadLocation;

            // make sure the datafolder still exists
            CreateDataFolder(blog.Name, blogPath);

            blog.ChildId = Path.Combine(indexPath, blog.Name + "_files.tumblr");
            TumblrFiles files = GetTumblrFiles(blog);

            var loopState = Parallel.ForEach(
                            bCollection.GetConsumingEnumerable(),
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
                                uint duplicates = blog.DuplicatePhotos + blog.DuplicateVideos + blog.DuplicateAudios;

                                switch (currentImageUrl.Item2)
                                {
                                    case "Photo":

                                        fileName = currentImageUrl.Item1.Split('/').Last();
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), fileName);

                                        if (Download(files, fileLocation, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedPhotos, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, fileName, lockObjectProgress, ref duplicates, ref downloadedImages);
                                            if (shellService.Settings.EnablePreview)
                                                blog.LastDownloadedPhoto = Path.GetFullPath(fileLocation);
                                            blog.DownloadedPhotos = (uint)downloadedPhotos;
                                        }                                    
                                        break;
                                    case "Video":
                                        fileName = currentImageUrl.Item1.Split('/').Last();
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), fileName);

                                        if (Download(files, fileLocation, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedVideos, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, fileName, lockObjectProgress, ref duplicates, ref downloadedImages);
                                            if (shellService.Settings.EnablePreview)
                                                blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);
                                            blog.DownloadedVideos = (uint)downloadedVideos;
                                        }
                                        break;
                                    case "Audio":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), currentImageUrl.Item3 + ".swf");
                                        if (Download(files, fileLocation, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedAudios, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item1, lockObjectProgress, ref duplicates, ref downloadedImages);
                                            blog.DownloadedAudios = (uint)downloadedAudios;
                                        }
                                        break;
                                    case "Text":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameTexts));

                                        if (Download(files, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedTexts, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item3, lockObjectProgress, ref duplicates, ref downloadedImages);
                                            blog.DownloadedTexts = (uint)downloadedTexts;
                                        }
                                        break;
                                    case "Quote":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameQuotes));

                                        if (Download(files, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedQuotes, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item3, lockObjectProgress, ref duplicates, ref downloadedImages);
                                            blog.DownloadedQuotes = (uint)downloadedQuotes;
                                        }
                                        break;
                                    case "Link":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameLinks));

                                        if (Download(files, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedLinks, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item3, lockObjectProgress, ref duplicates, ref downloadedImages);
                                            blog.DownloadedLinks = (uint)downloadedLinks;
                                        }
                                        break;
                                    case "Conversation":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameConversations));

                                        if (Download(files, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedConversations, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item3, lockObjectProgress, ref duplicates, ref downloadedImages);
                                            blog.DownloadedConversations = (uint)downloadedConversations;
                                        }
                                        break;
                                    case "PhotoMeta":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaPhoto));

                                        if (Download(files, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedPhotoMetas, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item3, lockObjectProgress, ref duplicates, ref downloadedImages);
                                            blog.DownloadedPhotoMetas = (uint)downloadedPhotoMetas;
                                        }
                                        break;
                                    case "VideoMeta":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaVideo));

                                        if (Download(files, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedVideoMetas, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item3, lockObjectProgress, ref duplicates, ref downloadedImages);
                                            blog.DownloadedVideoMetas = (uint)downloadedVideoMetas;
                                        }
                                        break;
                                    case "AudioMeta":
                                        fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaAudio));

                                        if (Download(files, fileLocation, currentImageUrl.Item3, currentImageUrl.Item1, progress, lockObjectDownload, locked, ref downloadedAudioMetas, ref downloadedImages))
                                        {
                                            UpdateProgress(blog, files, currentImageUrl.Item3, lockObjectProgress, ref duplicates, ref downloadedImages);
                                            blog.DownloadedAudioMetas = (uint)downloadedAudioMetas;
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            });

            blog.LastDownloadedPhoto = null;
            blog.LastDownloadedVideo = null;

            SaveTumblrFiles(files);
            files = null;

            if (loopState.IsCompleted)
                return true;

            return false;
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

                XDocument blogDoc = null;

                if (shellService.Settings.LimitConnections)
                    blogDoc = timeconstraint.Perform<XDocument>(() => RequestData(url)).Result;
                else
                    blogDoc = RequestData(url);

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
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
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
                if (ex.Message.Contains("429"))
                {
                    Logger.Error("ManagerController:RequestData: {0}", ex);
                    shellService.ShowError(ex, Resources.LimitExceeded, new Uri(url).Host);
                }
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

            TumblrFiles files = new TumblrFiles();
            files.Name = blogName + "_files";

            lock (lockObject)
            {
                if (selectionService.BlogFiles.Select(blogs => blogs.Name).ToList().Contains(blogName))
                {
                    shellService.ShowError(null, Resources.BlogAlreadyExist, blogName);
                    return;
                }

                if (SaveBlog(blog) && SaveTumblrFiles(files))
                {

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
                    System.Web.Script.Serialization.JavaScriptSerializer jsJson = new System.Web.Script.Serialization.JavaScriptSerializer();
                    jsJson.MaxJsonLength = 2147483644;
                    File.WriteAllText(newIndex, jsJson.Serialize(blog));
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
                    System.Web.Script.Serialization.JavaScriptSerializer jsJson = new System.Web.Script.Serialization.JavaScriptSerializer();
                    jsJson.MaxJsonLength = 2147483644;
                    File.WriteAllText(currentIndex, jsJson.Serialize(blog));
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

        public bool SaveTumblrFiles(TumblrFiles files)
        {
            string currentIndex, newIndex, backupIndex;
            if (files == null)
                return false;

            var indexPath = Path.Combine(shellService.Settings.DownloadLocation, "Index");
            var blogPath = shellService.Settings.DownloadLocation;

            if (files.Name.EndsWith(".tumblr", System.StringComparison.CurrentCultureIgnoreCase))
            {
                currentIndex =  files.Name ;
                newIndex =  files.Name + ".new";
                backupIndex = files.Name + ".bak";

            }
            else
            {
                currentIndex = Path.Combine(indexPath, files.Name + ".tumblr");
                newIndex = Path.Combine(indexPath, files.Name + ".tumblr.new");
                backupIndex = Path.Combine(indexPath, files.Name + ".tumblr.bak");
            }
            
            try
            {

                if (File.Exists(currentIndex))
                {
                    System.Web.Script.Serialization.JavaScriptSerializer jsJson = new System.Web.Script.Serialization.JavaScriptSerializer();
                    jsJson.MaxJsonLength = 2147483644;
                    File.WriteAllText(newIndex, jsJson.Serialize(files)); File.Replace(newIndex, currentIndex, backupIndex, true);
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
                    System.Web.Script.Serialization.JavaScriptSerializer jsJson = new System.Web.Script.Serialization.JavaScriptSerializer();
                    jsJson.MaxJsonLength = 2147483644;
                    File.WriteAllText(currentIndex, jsJson.Serialize(files));
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("ManagerController:SaveBlog: {0}", ex);
                shellService.ShowError(ex, Resources.CouldNotSaveBlog, files.Name);
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

                XDocument blogDoc = null;

                if (shellService.Settings.LimitConnections)
                    blogDoc = timeconstraint.Perform<XDocument>(() => RequestData(url)).Result;
                else
                    blogDoc = RequestData(url);

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

        private bool Download(TumblrFiles blog, string fileLocation, string url, IProgress<DataModels.DownloadProgress> progress, object lockObject, bool locked, ref int counter, ref int totalCounter)
        {
            var fileName = url.Split('/').Last();
            Monitor.Enter(lockObject, ref locked);
            if (blog.Links.Contains(fileName))
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
                    newProgress.Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressDownloadImage, fileName); ;
                    progress.Report(newProgress);

                    using (var stream = ThrottledStream.ReadFromURLIntoStream(url, (shellService.Settings.Bandwidth / shellService.Settings.ParallelImages), shellService.Settings.TimeOut, shellService.Settings.ProxyHost, shellService.Settings.ProxyPort))
                        ThrottledStream.SaveStreamToDisk(stream, fileLocation);
                    Interlocked.Increment(ref counter);
                    Interlocked.Increment(ref totalCounter);
                    return true;
                }
                catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x27 || (ex.HResult & 0xFFFF) == 0x70)
                {
                    Logger.Error("ManagerController:Download: {0}", ex);
                    shellService.ShowError(ex, Resources.DiskFull);
                    if (stopCommand.CanExecute(null))
                        stopCommand.Execute(null);
                    return false;
                }
                catch 
                {
                    return false;
                }
            }
        }

        private bool Download(TumblrFiles blog, string fileLocation, string postId, string text, IProgress<DataModels.DownloadProgress> progress, object lockObject, bool locked, ref int counter, ref int totalCounter)
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
                catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x27 || (ex.HResult & 0xFFFF) == 0x70)
                {
                    Logger.Error("ManagerController:Download: {0}", ex);
                    shellService.ShowError(ex, Resources.DiskFull);
                    if (stopCommand.CanExecute(null))
                        stopCommand.Execute(null);
                    return false;
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
