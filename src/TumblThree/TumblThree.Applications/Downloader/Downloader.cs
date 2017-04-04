using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Downloader
{
    public abstract class Downloader
    {
        private readonly IBlog blog;
        protected readonly IFiles files;
        private readonly PostCounter counter;
        private readonly IShellService shellService;
        private readonly ICrawlerService crawlerService;
        protected readonly object lockObjectDb;
        private readonly object lockObjectDirectory;
        private readonly object lockObjectDownload;
        private readonly object lockObjectProgress;
        protected readonly ConcurrentBag<Tuple<PostTypes, string, string>> statisticsBag;
        protected readonly BlockingCollection<Tuple<PostTypes, string, string>> producerConsumerCollection;

        protected Downloader(IShellService shellService, ICrawlerService crawlerService = null, IBlog blog = null)
        {
            this.shellService = shellService;
            this.crawlerService = crawlerService;
            this.blog = blog;
            this.counter = new PostCounter(blog);
            this.files = LoadFiles();
            this.lockObjectDb = new object();
            this.lockObjectDirectory = new object();
            this.lockObjectDownload = new object();
            this.lockObjectProgress = new object();
            this.statisticsBag = new ConcurrentBag<Tuple<PostTypes, string, string>>();
            this.producerConsumerCollection = new BlockingCollection<Tuple<PostTypes, string, string>>();
            SetUp();
        }

        private IFiles LoadFiles()
        {
            string filename = blog.ChildId;

            try
            {
                string json = File.ReadAllText(filename);
                var jsJson =
                    new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = 2147483644 };
                return jsJson.Deserialize<Files>(json);
            }
            catch (InvalidOperationException ex)
            {
                ex.Data["Filename"] = filename;
                throw;
            }
        }

        protected virtual async Task<string> RequestDataAsync(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ProtocolVersion = HttpVersion.Version11;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
            request.AllowAutoRedirect = true;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ReadWriteTimeout = shellService.Settings.TimeOut * 1000;
            request.Timeout = -1;
            ServicePointManager.DefaultConnectionLimit = 400;
            if (!String.IsNullOrEmpty(shellService.Settings.ProxyHost))
            {
                request.Proxy = new WebProxy(shellService.Settings.ProxyHost, Int32.Parse(shellService.Settings.ProxyPort));
            }
            else
            {
                request.Proxy = null;
            }

            int bandwidth = 2000000;
            if (shellService.Settings.LimitScanBandwidth)
            {
                bandwidth = shellService.Settings.Bandwidth;
            }

            using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
            {
                using (ThrottledStream stream = new ThrottledStream(response.GetResponseStream(), (bandwidth / shellService.Settings.ParallelImages) * 1024))
                {
                    using (BufferedStream buffer = new BufferedStream(stream))
                    {
                        using (StreamReader reader = new StreamReader(buffer))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }

        protected static string UrlEncode(IDictionary<string, string> parameters)
        {
            var sb = new StringBuilder();
            foreach (var val in parameters)
            {
                sb.AppendFormat("{0}={1}&", val.Key, HttpUtility.UrlEncode(val.Value));
            }
            sb.Remove(sb.Length - 1, 1); // remove last '&'
            return sb.ToString();
        }

        private bool CreateDataFolder()
        {
            if (string.IsNullOrEmpty(blog.Name))
                return false;

            string blogPath = Directory.GetParent(blog.Location).FullName;

            if (!Directory.Exists(Path.Combine(blogPath, blog.Name)))
            {
                Directory.CreateDirectory(Path.Combine(blogPath, blog.Name));
                return true;
            }
            return true;
        }

        public async Task IsBlogOnlineAsync()
        {
            try
            {
                await RequestDataAsync(blog.Url);
                blog.Online = true;
            }
            catch (WebException)
            {
                blog.Online = false;
            }
        }

        public virtual async Task UpdateMetaInformationAsync()
        {
            await Task.FromResult<object>(null);
        }

        private async void SetUp()
        {
            CreateDataFolder();
            await IsBlogOnlineAsync();
        }

        protected virtual bool CheckIfFileExistsInDB(string url)
        {
            string fileName = url.Split('/').Last();
            Monitor.Enter(lockObjectDb);
            if (blog.Links.Contains(fileName))
            {
                Monitor.Exit(lockObjectDb);
                return true;
            }
            Monitor.Exit(lockObjectDb);
            return false;
        }

        protected virtual bool CheckIfBlogShouldCheckDirectory(string url)
        {
            if (blog.CheckDirectoryForFiles)
            {
                return CheckIfFileExistsInDirectory(url);
            }
            return false;
        }

        protected virtual bool CheckIfFileExistsInDirectory(string url)
        {
            string fileName = url.Split('/').Last();
            Monitor.Enter(lockObjectDirectory);
            string blogPath = Path.Combine(Directory.GetParent(blog.Location).FullName, blog.Name);
            if (File.Exists(Path.Combine(blogPath, fileName)))
            {
                Monitor.Exit(lockObjectDirectory);
                return true;
            }
            Monitor.Exit(lockObjectDirectory);
            return false;
        }

        protected virtual void UpdateProgressQueueInformation(IProgress<DataModels.DownloadProgress> progress, string format, params object[] args)
        {
            var newProgress = new DataModels.DownloadProgress
            {
                Progress = string.Format(CultureInfo.CurrentCulture, format, args)
            };
            progress.Report(newProgress);
        }

        protected virtual string GetCoreImageUrl(string url)
        {
            return url;
        }

        protected virtual async Task<bool> DownloadBinaryFile(string fileLocation, string url)
        {
            try
            {
                using (ThrottledStream stream = await ThrottledStream.ReadFromURLIntoStream(url,
                    (shellService.Settings.Bandwidth / shellService.Settings.ParallelImages),
                    shellService.Settings.TimeOut, shellService.Settings.ProxyHost,
                    shellService.Settings.ProxyPort))
                    ThrottledStream.SaveStreamToDisk(stream, fileLocation);
                return true;
            }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x27 || (ex.HResult & 0xFFFF) == 0x70)
            {
                Logger.Error("ManagerController:Download: {0}", ex);
                shellService.ShowError(ex, Resources.DiskFull);
                crawlerService.StopCommand.Execute(null);
                return false;
            }
            catch
            {
                return false;
            }
        }

        protected virtual async Task<bool> DownloadBinaryFile(string fileLocation, string fileLocationUrlList, string url)
        {
            if (!blog.DownloadUrlList)
            {
                return await DownloadBinaryFile(fileLocation, url);
            }
            else
            {
                return AppendToTextFile(fileLocationUrlList, url);
            }
        }

        protected virtual bool AppendToTextFile(string fileLocation, string text)
        {
            try
            {
                // better not await in the lock?
                lock (lockObjectDownload)
                {
                    using (var sw = new StreamWriter(fileLocation, true))
                    {
                        sw.WriteLine(text);
                    }
                }
                return true;
            }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x27 || (ex.HResult & 0xFFFF) == 0x70)
            {
                Logger.Error("ManagerController:Download: {0}", ex);
                shellService.ShowError(ex, Resources.DiskFull);
                crawlerService.StopCommand.Execute(null);
                return false;
            }
            catch
            {
                return false;
            }
        }

        protected virtual void UpdateBlogProgress(ref int totalCounter)
        {
            blog.DownloadedImages = Interlocked.Increment(ref totalCounter);
            blog.Progress = (int)((double)totalCounter / (double)blog.TotalCount * 100);
        }

        protected virtual void UpdateBlogPostCount(ref int postCounter, Action<int> blogCounter)
        {
            blogCounter(Interlocked.Increment(ref postCounter));
        }

        protected virtual void UpdateBlogDB(string fileName)
        {
            lock (lockObjectProgress)
            {
                files.Links.Add(fileName);
            }
        }

        protected virtual async Task<bool> DownloadBlog(IProgress<DataModels.DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            SemaphoreSlim semaphoreSlim = new SemaphoreSlim(shellService.Settings.ParallelImages / crawlerService.ActiveItems.Count);
            List<Task> trackedTasks = new List<Task>();
            bool completeDownload = true;

            CreateDataFolder();

            foreach (var downloadItem in producerConsumerCollection.GetConsumingEnumerable())
            {
                await semaphoreSlim.WaitAsync();

                if (ct.IsCancellationRequested)
                {
                    completeDownload = false;
                    break;
                }
                if (pt.IsPaused)
                    pt.WaitWhilePausedWithResponseAsyc().Wait();

                trackedTasks.Add(new Func<Task>(async () =>
                {
                    switch (downloadItem.Item1)
                    {
                        case PostTypes.Photo:
                            await DownloadPhotoAsync(progress, downloadItem);
                            break;
                        case PostTypes.Video:
                            await DownloadVideoAsync(progress, downloadItem);
                            break;
                        case PostTypes.Audio:
                            await DownloadAudioAsync(progress, downloadItem);
                            break;
                        case PostTypes.Text:
                            DownloadText(progress, downloadItem);
                            break;
                        case PostTypes.Quote:
                            DownloadQuote(progress, downloadItem);
                            break;
                        case PostTypes.Link:
                            DownloadLink(progress, downloadItem);
                            break;
                        case PostTypes.Conversation:
                            DownloadConversation(progress, downloadItem);
                            break;
                        case PostTypes.PhotoMeta:
                            DownloadPhotoMeta(progress, downloadItem);
                            break;
                        case PostTypes.VideoMeta:
                            DownloadVideoMeta(progress, downloadItem);
                            break;
                        case PostTypes.AudioMeta:
                            DownloadAudioMeta(progress, downloadItem);
                            break;
                        default:
                            break;
                    }
                    semaphoreSlim.Release();
                })());
            }
            await Task.WhenAll(trackedTasks);

            blog.LastDownloadedPhoto = null;
            blog.LastDownloadedVideo = null;

            files.Save();

            return completeDownload;
        }



        private async Task DownloadPhotoAsync(IProgress<DataModels.DownloadProgress> progress, Tuple<PostTypes, string, string> downloadItem)
        {
            string blogDownloadLocation = blog.DownloadLocation();
            string fileName = FileName(downloadItem);
            string url = Url(downloadItem);
            string fileLocation = FileLocation(blogDownloadLocation, fileName);
            string fileLocationUrlList = FileLocationLocalized(blogDownloadLocation, Resources.FileNamePhotos);

            if (!(CheckIfFileExistsInDB(url) || CheckIfBlogShouldCheckDirectory(GetCoreImageUrl(url))))
            {
                UpdateProgressQueueInformation(progress, Resources.ProgressDownloadImage, fileName);
                if (await DownloadBinaryFile(fileLocation, fileLocationUrlList, url))
                {
                    UpdateBlogPostCount(ref counter.Photos, value => blog.DownloadedPhotos = value);
                    UpdateBlogProgress(ref counter.TotalDownloads);
                    UpdateBlogDB(fileName);
                    if (shellService.Settings.EnablePreview)
                    {
                        if (!fileName.EndsWith(".gif"))
                            blog.LastDownloadedPhoto = Path.GetFullPath(fileLocation);
                        else
                            blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);
                    }
                }
            }
        }

        private async Task DownloadVideoAsync(IProgress<DataModels.DownloadProgress> progress, Tuple<PostTypes, string, string> downloadItem)
        {
            string blogDownloadLocation = blog.DownloadLocation();
            string fileName = FileName(downloadItem);
            string url = Url(downloadItem);
            string fileLocation = FileLocation(blogDownloadLocation, fileName);
            string fileLocationUrlList = FileLocationLocalized(blogDownloadLocation, Resources.FileNameVideos);

            if (!(CheckIfFileExistsInDB(url) || CheckIfBlogShouldCheckDirectory(url)))
            {
                UpdateProgressQueueInformation(progress, Resources.ProgressDownloadImage, fileName);
                if (await DownloadBinaryFile(fileLocation, fileLocationUrlList, url))
                {
                    UpdateBlogPostCount(ref counter.Videos, value => blog.DownloadedVideos = value);
                    UpdateBlogProgress(ref counter.TotalDownloads);
                    UpdateBlogDB(fileName);
                    if (shellService.Settings.EnablePreview)
                    {
                        blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);
                    }
                }
            }
        }

        private async Task DownloadAudioAsync(IProgress<DataModels.DownloadProgress> progress, Tuple<PostTypes, string, string> downloadItem)
        {
            string blogDownloadLocation = blog.DownloadLocation();
            string fileName = FileName(downloadItem);
            string url = Url(downloadItem);
            string fileLocation = FileLocation(blogDownloadLocation, downloadItem.Item3 + ".swf");
            string fileLocationUrlList = FileLocationLocalized(blogDownloadLocation, Resources.FileNameAudios);

            if (!(CheckIfFileExistsInDB(url) || CheckIfBlogShouldCheckDirectory(url)))
            {
                UpdateProgressQueueInformation(progress, Resources.ProgressDownloadImage, fileName);
                if (await DownloadBinaryFile(fileLocation, fileLocationUrlList, url))
                {
                    UpdateBlogPostCount(ref counter.Audios, value => blog.DownloadedAudios = value);
                    UpdateBlogProgress(ref counter.TotalDownloads);
                    UpdateBlogDB(fileName);
                }
            }
        }

        private void DownloadText(IProgress<DataModels.DownloadProgress> progress, Tuple<PostTypes, string, string> downloadItem)
        {
            string blogDownloadLocation = blog.DownloadLocation();
            string url = Url(downloadItem);
            string postId = PostId(downloadItem);
            string fileLocation = FileLocationLocalized(blogDownloadLocation, Resources.FileNameTexts);

            if (!CheckIfFileExistsInDB(postId))
            {
                UpdateProgressQueueInformation(progress, Resources.ProgressDownloadImage, postId);
                if (AppendToTextFile(fileLocation, url))
                {
                    UpdateBlogPostCount(ref counter.Audios, value => blog.DownloadedTexts = value);
                    UpdateBlogProgress(ref counter.TotalDownloads);
                    UpdateBlogDB(postId);
                }
            }
        }

        private void DownloadQuote(IProgress<DataModels.DownloadProgress> progress, Tuple<PostTypes, string, string> downloadItem)
        {
            string blogDownloadLocation = blog.DownloadLocation();
            string url = Url(downloadItem);
            string postId = PostId(downloadItem);
            string fileLocation = FileLocationLocalized(blogDownloadLocation, Resources.FileNameQuotes);

            if (!CheckIfFileExistsInDB(postId))
            {
                UpdateProgressQueueInformation(progress, Resources.ProgressDownloadImage, postId);
                if (AppendToTextFile(fileLocation, url))
                {
                    UpdateBlogPostCount(ref counter.Quotes, value => blog.DownloadedQuotes = value);
                    UpdateBlogProgress(ref counter.TotalDownloads);
                    UpdateBlogDB(postId);
                }
            }
        }
        private void DownloadLink(IProgress<DataModels.DownloadProgress> progress, Tuple<PostTypes, string, string> downloadItem)
        {
            string blogDownloadLocation = blog.DownloadLocation();
            string url = Url(downloadItem);
            string postId = PostId(downloadItem);
            string fileLocation = FileLocationLocalized(blogDownloadLocation, Resources.FileNameLinks);

            if (!CheckIfFileExistsInDB(postId))
            {
                UpdateProgressQueueInformation(progress, Resources.ProgressDownloadImage, postId);
                if (AppendToTextFile(fileLocation, url))
                {
                    UpdateBlogPostCount(ref counter.Links, value => blog.DownloadedLinks = value);
                    UpdateBlogProgress(ref counter.TotalDownloads);
                    UpdateBlogDB(postId);
                }
            }
        }
        private void DownloadConversation(IProgress<DataModels.DownloadProgress> progress, Tuple<PostTypes, string, string> downloadItem)
        {
            string blogDownloadLocation = blog.DownloadLocation();
            string url = Url(downloadItem);
            string postId = PostId(downloadItem);
            string fileLocation = FileLocationLocalized(blogDownloadLocation, Resources.FileNameConversations);

            if (!CheckIfFileExistsInDB(postId))
            {
                UpdateProgressQueueInformation(progress, Resources.ProgressDownloadImage, postId);
                if (AppendToTextFile(fileLocation, url))
                {
                    UpdateBlogPostCount(ref counter.Conversations, value => blog.DownloadedConversations = value);
                    UpdateBlogProgress(ref counter.TotalDownloads);
                    UpdateBlogDB(postId);
                }
            }
        }
        private void DownloadPhotoMeta(IProgress<DataModels.DownloadProgress> progress, Tuple<PostTypes, string, string> downloadItem)
        {
            string blogDownloadLocation = blog.DownloadLocation();
            string url = Url(downloadItem);
            string postId = PostId(downloadItem);
            string fileLocation = FileLocationLocalized(blogDownloadLocation, Resources.FileNameMetaPhoto);

            if (!CheckIfFileExistsInDB(postId))
            {
                UpdateProgressQueueInformation(progress, Resources.ProgressDownloadImage, postId);
                if (AppendToTextFile(fileLocation, url))
                {
                    UpdateBlogPostCount(ref counter.PhotoMetas, value => blog.DownloadedPhotoMetas = value);
                    UpdateBlogProgress(ref counter.TotalDownloads);
                    UpdateBlogDB(postId);
                }
            }
        }
        private void DownloadVideoMeta(IProgress<DataModels.DownloadProgress> progress, Tuple<PostTypes, string, string> downloadItem)
        {
            string blogDownloadLocation = blog.DownloadLocation();
            string url = Url(downloadItem);
            string postId = PostId(downloadItem);
            string fileLocation = FileLocationLocalized(blogDownloadLocation, Resources.FileNameMetaVideo);

            if (!CheckIfFileExistsInDB(postId))
            {
                UpdateProgressQueueInformation(progress, Resources.ProgressDownloadImage, postId);
                if (AppendToTextFile(fileLocation, url))
                {
                    UpdateBlogPostCount(ref counter.VideoMetas, value => blog.DownloadedVideoMetas = value);
                    UpdateBlogProgress(ref counter.TotalDownloads);
                    UpdateBlogDB(postId);
                }
            }
        }
        private void DownloadAudioMeta(IProgress<DataModels.DownloadProgress> progress, Tuple<PostTypes, string, string> downloadItem)
        {
            string blogDownloadLocation = blog.DownloadLocation();
            string url = Url(downloadItem);
            string postId = PostId(downloadItem);
            string fileLocation = FileLocationLocalized(blogDownloadLocation, Resources.FileNameMetaAudio);

            if (!CheckIfFileExistsInDB(postId))
            {
                UpdateProgressQueueInformation(progress, Resources.ProgressDownloadImage, postId);
                if (AppendToTextFile(fileLocation, url))
                {
                    UpdateBlogPostCount(ref counter.AudioMetas, value => blog.DownloadedAudioMetas = value);
                    UpdateBlogProgress(ref counter.TotalDownloads);
                    UpdateBlogDB(postId);
                }
            }
        }

        private static string Url(Tuple<PostTypes, string, string> downloadItem)
        {
            return downloadItem.Item2;
        }

        private static string FileName(Tuple<PostTypes, string, string> downloadItem)
        {
            return downloadItem.Item2.Split('/').Last();
        }

        private static string FileLocation(string blogDownloadLocation, string fileName)
        {
            return Path.Combine(blogDownloadLocation, fileName);
        }

        private static string FileLocationLocalized(string blogDownloadLocation, string fileName)
        {
            return Path.Combine(blogDownloadLocation, string.Format(CultureInfo.CurrentCulture, fileName));
        }

        private static string PostId(Tuple<PostTypes, string, string> downloadItem)
        {
            return downloadItem.Item3;
        }
    }
}
