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
        private readonly IShellService shellService;
        private readonly ICrawlerService crawlerService;
        protected readonly object lockObjectDb;
        protected readonly object lockObjectDirectory;
        protected readonly object lockObjectDownload;
        private readonly object lockObjectProgress;
        protected readonly List<Tuple<PostTypes, string, string>> downloadList;
        protected readonly BlockingCollection<Tuple<PostTypes, string, string>> sharedDownloads;

        public Downloader(IShellService shellService): this(shellService, null, null)
        {
        }

        public Downloader(IShellService shellService, ICrawlerService crawlerService, IBlog blog)
        {
            this.shellService = shellService;
            this.crawlerService = crawlerService;
            this.blog = blog;
            this.files = LoadFiles();
            this.lockObjectDb = new object();
            this.lockObjectDirectory = new object();
            this.lockObjectDownload = new object();
            this.lockObjectProgress = new object();
            this.downloadList = new List<Tuple<PostTypes, string, string>>();
            this.sharedDownloads = new BlockingCollection<Tuple<PostTypes, string, string>>();
            SetUp();
        }

        private IFiles LoadFiles()
        {
            string filename = blog.ChildId;

            try
            {
                string json = File.ReadAllText(filename);
                System.Web.Script.Serialization.JavaScriptSerializer jsJson = new System.Web.Script.Serialization.JavaScriptSerializer();
                jsJson.MaxJsonLength = 2147483644;
                IFiles files = jsJson.Deserialize<Files>(json);
                return files;
            }
            catch (InvalidOperationException ex)
            {
                ex.Data["Filename"] = filename;
                throw;
            }
        }

        protected virtual async Task<string> RequestDataAsync(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.ProtocolVersion = HttpVersion.Version11;
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                request.ContentType = "x-www-from-urlencoded";
                request.KeepAlive = false;
                request.ServicePoint.Expect100Continue = false;
                request.AllowAutoRedirect = true;
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.Pipelined = true;
                request.Timeout = shellService.Settings.TimeOut * 1000;
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
            catch
            {
                return null;
            }
        }

        protected string UrlEncode(IDictionary<string, string> parameters)
        {
            var sb = new StringBuilder();
            foreach (var val in parameters)
            {
                sb.AppendFormat("{0}={1}&", val.Key, HttpUtility.UrlEncode(val.Value));
            }
            sb.Remove(sb.Length - 1, 1); // remove last '&'
            return sb.ToString();
        }

        protected bool CreateDataFolder()
        {
            if (String.IsNullOrEmpty(blog.Name))
                return false;

            string blogPath = Directory.GetParent(blog.Location).FullName;

            if (!Directory.Exists(Path.Combine(blogPath, blog.Name)))
            {
                Directory.CreateDirectory(Path.Combine(blogPath, blog.Name));
                return true;
            }
            return true;
        }

        public virtual async Task IsBlogOnlineAsync()
        {
            string request = await RequestDataAsync(blog.Url);

            if (request != null)
                blog.Online = true;
            else
                blog.Online = false;
        }

        public virtual async Task UpdateMetaInformationAsync()
        {
        }

        protected virtual async void SetUp()
        {
            CreateDataFolder();
            await IsBlogOnlineAsync();
        }

        protected virtual bool CheckIfFileExistsInDB(string url)
        {
            var fileName = url.Split('/').Last();
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
            var fileName = url.Split('/').Last();
            Monitor.Enter(lockObjectDirectory);
            string blogPath = Path.Combine(Directory.GetParent(blog.Location).FullName, blog.Name);
            if (System.IO.File.Exists(Path.Combine(blogPath, fileName)))
            {
                Monitor.Exit(lockObjectDirectory);
                return true;
            }
            Monitor.Exit(lockObjectDirectory);
            return false;
        }

        protected virtual void UpdateProgressQueueInformation(IProgress<DataModels.DownloadProgress> progress, string fileName)
        {
            var newProgress = new DataModels.DownloadProgress();
            newProgress.Progress = string.Format(CultureInfo.CurrentCulture, Resources.ProgressDownloadImage, fileName);
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
                using (var stream = await ThrottledStream.ReadFromURLIntoStream(url,
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
                throw;
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
                return await AppendToTextFile(fileLocationUrlList, url);
            }
        }

        protected virtual async Task<bool> AppendToTextFile(string fileLocation, string text)
        {
            try
            {
                // better not await in the lock?
                lock (lockObjectDownload)
                {
                    using (StreamWriter sw = new StreamWriter(fileLocation, true))
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
                throw;
            }
            catch
            {
                return false;
            }
        }

        protected virtual void UpdateBlogCounter(ref int counter, ref int totalCounter)
        {
            Interlocked.Increment(ref counter);
            Interlocked.Increment(ref totalCounter);
        }

        protected virtual void UpdateBlogProgress(string fileName, ref int totalCounter)
        {
            lock (lockObjectProgress)
            {
                files.Links.Add(fileName);
                blog.DownloadedImages = totalCounter;
                blog.Progress = (int)((double)totalCounter / (double)blog.TotalCount * 100);
            }
        }

        protected virtual async Task<bool> DownloadBlog(IProgress<DataModels.DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            SemaphoreSlim semaphoreSlim = new SemaphoreSlim(shellService.Settings.ParallelImages / crawlerService.ActiveItems.Count);
            List<Task> trackedTasks = new List<Task>();
            PostCounter counter = new PostCounter(blog);
            bool loopCompleted = true;

            string blogPath = Directory.GetParent(blog.Location).FullName;

            CreateDataFolder();

            // Not sure if this is any better than the Parallel.For with synchronous code
            // since this still seems to run on the thread pool.
            foreach (var currentImageUrl in sharedDownloads.GetConsumingEnumerable())
            {
                await semaphoreSlim.WaitAsync();

                if (ct.IsCancellationRequested)
                {
                    break;
                }
                if (pt.IsPaused)
                    pt.WaitWhilePausedWithResponseAsyc().Wait();

                trackedTasks.Add(new Func<Task>(async () =>
                {
                    string fileName = String.Empty;
                    string url = String.Empty;
                    string fileLocation = String.Empty;
                    string fileLocationUrlList = String.Empty;
                    string postId = String.Empty;

                    // FIXME: Conditional with Polymorphism
                    switch (currentImageUrl.Item1)
                    {
                        case PostTypes.Photo:
                            fileName = currentImageUrl.Item2.Split('/').Last();
                            url = currentImageUrl.Item2;
                            fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), fileName);
                            fileLocationUrlList = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNamePhotos));

                            if (!(CheckIfFileExistsInDB(url) || CheckIfBlogShouldCheckDirectory(GetCoreImageUrl(url))))
                            {
                                UpdateProgressQueueInformation(progress, fileName);
                                await DownloadBinaryFile(fileLocation, fileLocationUrlList, url);
                                UpdateBlogCounter(ref counter.Photos, ref counter.TotalDownloads);
                                UpdateBlogProgress(fileName, ref counter.TotalDownloads);
                                blog.DownloadedPhotos = counter.Photos;
                                if (shellService.Settings.EnablePreview)
                                {
                                    if (!fileName.EndsWith(".gif"))
                                        blog.LastDownloadedPhoto = Path.GetFullPath(fileLocation);
                                    else
                                        blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);
                                }
                            }
                            break;
                        case PostTypes.Video:
                            fileName = currentImageUrl.Item2.Split('/').Last();
                            url = currentImageUrl.Item2;
                            fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), fileName);
                            fileLocationUrlList = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameVideos));

                            if (!(CheckIfFileExistsInDB(url) || CheckIfBlogShouldCheckDirectory(url)))
                            {
                                UpdateProgressQueueInformation(progress, fileName);
                                await DownloadBinaryFile(fileLocation, fileLocationUrlList, url);
                                UpdateBlogCounter(ref counter.Videos, ref counter.TotalDownloads);
                                UpdateBlogProgress(fileName, ref counter.TotalDownloads);
                                blog.DownloadedVideos = counter.Videos;
                                if (shellService.Settings.EnablePreview)
                                {
                                    blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);
                                }
                            }
                            break;
                        case PostTypes.Audio:
                            fileName = currentImageUrl.Item2.Split('/').Last();
                            url = currentImageUrl.Item2;
                            fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), currentImageUrl.Item3 + ".swf");
                            fileLocationUrlList = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameAudios));

                            if (!(CheckIfFileExistsInDB(url) || CheckIfBlogShouldCheckDirectory(url)))
                            {
                                UpdateProgressQueueInformation(progress, fileName);
                                await DownloadBinaryFile(fileLocation, fileLocationUrlList, url);
                                UpdateBlogCounter(ref counter.Audios, ref counter.TotalDownloads);
                                UpdateBlogProgress(fileName, ref counter.TotalDownloads);
                                blog.DownloadedAudios = counter.Audios;
                            }
                            break;
                        case PostTypes.Text:
                            url = currentImageUrl.Item2;
                            postId = currentImageUrl.Item3;
                            fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameTexts));

                            if (!CheckIfFileExistsInDB(postId))
                            {
                                UpdateProgressQueueInformation(progress, postId);
                                await AppendToTextFile(fileLocation, url);
                                UpdateBlogCounter(ref counter.Texts, ref counter.TotalDownloads);
                                UpdateBlogProgress(postId, ref counter.TotalDownloads);
                                blog.DownloadedTexts = counter.Texts;
                            }
                            break;
                        case PostTypes.Quote:
                            url = currentImageUrl.Item2;
                            postId = currentImageUrl.Item3;
                            fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameQuotes));

                            if (!CheckIfFileExistsInDB(postId))
                            {
                                UpdateProgressQueueInformation(progress, postId);
                                await AppendToTextFile(fileLocation, url);
                                UpdateBlogCounter(ref counter.Quotes, ref counter.TotalDownloads);
                                UpdateBlogProgress(postId, ref counter.TotalDownloads);
                                blog.DownloadedQuotes = counter.Quotes;
                            }
                            break;
                        case PostTypes.Link:
                            url = currentImageUrl.Item2;
                            postId = currentImageUrl.Item3;
                            fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameLinks));

                            if (!CheckIfFileExistsInDB(postId))
                            {
                                UpdateProgressQueueInformation(progress, postId);
                                await AppendToTextFile(fileLocation, url);
                                UpdateBlogCounter(ref counter.Links, ref counter.TotalDownloads);
                                UpdateBlogProgress(postId, ref counter.TotalDownloads);
                                blog.DownloadedLinks = counter.Links;
                            }
                            break;
                        case PostTypes.Conversation:
                            url = currentImageUrl.Item2;
                            postId = currentImageUrl.Item3;
                            fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameConversations));

                            if (!CheckIfFileExistsInDB(postId))
                            {
                                UpdateProgressQueueInformation(progress, postId);
                                await AppendToTextFile(fileLocation, url);
                                UpdateBlogCounter(ref counter.Conversations, ref counter.TotalDownloads);
                                UpdateBlogProgress(postId, ref counter.TotalDownloads);
                                blog.DownloadedConversations = counter.Conversations;
                            }
                            break;
                        case PostTypes.PhotoMeta:
                            url = currentImageUrl.Item2;
                            postId = currentImageUrl.Item3;
                            fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaPhoto));

                            if (!CheckIfFileExistsInDB(postId))
                            {
                                UpdateProgressQueueInformation(progress, postId);
                                await AppendToTextFile(fileLocation, url);
                                UpdateBlogCounter(ref counter.PhotoMetas, ref counter.TotalDownloads);
                                UpdateBlogProgress(postId, ref counter.TotalDownloads);
                                blog.DownloadedPhotoMetas = counter.PhotoMetas;
                            }
                            break;
                        case PostTypes.VideoMeta:
                            url = currentImageUrl.Item2;
                            postId = currentImageUrl.Item3;
                            fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaVideo));

                            if (!CheckIfFileExistsInDB(postId))
                            {
                                UpdateProgressQueueInformation(progress, postId);
                                await AppendToTextFile(fileLocation, url);
                                UpdateBlogCounter(ref counter.VideoMetas, ref counter.TotalDownloads);
                                UpdateBlogProgress(postId, ref counter.TotalDownloads);
                                blog.DownloadedVideoMetas = counter.VideoMetas;
                            }
                            break;
                        case PostTypes.AudioMeta:
                            url = currentImageUrl.Item2;
                            postId = currentImageUrl.Item3;
                            fileLocation = Path.Combine(Path.Combine(blogPath, blog.Name), string.Format(CultureInfo.CurrentCulture, Resources.FileNameMetaAudio));

                            if (!CheckIfFileExistsInDB(postId))
                            {
                                UpdateProgressQueueInformation(progress, postId);
                                await AppendToTextFile(fileLocation, url);
                                UpdateBlogCounter(ref counter.AudioMetas, ref counter.TotalDownloads);
                                UpdateBlogProgress(postId, ref counter.TotalDownloads);
                                blog.DownloadedAudioMetas = counter.AudioMetas;
                            }
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

            if (loopCompleted)
                return true;

            return false;
        }
    }
}
