using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Applications.Downloader
{
    public abstract class AbstractDownloader : IDownloader
    {
        protected readonly IBlog blog;
        protected readonly IFiles files;
        protected readonly ICrawlerService crawlerService;
        private readonly IManagerService managerService;
        protected readonly IProgress<DownloadProgress> progress;
        protected readonly object lockObjectDownload = new object();
        protected readonly IPostQueue<TumblrPost> postQueue;
        protected readonly IShellService shellService;
        protected readonly CancellationToken ct;
        protected readonly PauseToken pt;
        protected readonly FileDownloader fileDownloader;
        private readonly string[] suffixes = { ".jpg", ".jpeg", ".png" };

        private SemaphoreSlim concurrentConnectionsSemaphore;
        private SemaphoreSlim concurrentVideoConnectionsSemaphore;

        protected AbstractDownloader(IShellService shellService, IManagerService managerService, CancellationToken ct,
            PauseToken pt, IProgress<DownloadProgress> progress, IPostQueue<TumblrPost> postQueue, FileDownloader fileDownloader,
            ICrawlerService crawlerService = null, IBlog blog = null, IFiles files = null)
        {
            this.shellService = shellService;
            this.crawlerService = crawlerService;
            this.managerService = managerService;
            this.blog = blog;
            this.files = files;
            this.ct = ct;
            this.pt = pt;
            this.progress = progress;
            this.postQueue = postQueue;
            this.fileDownloader = fileDownloader;
        }

        public void UpdateProgressQueueInformation(string format, params object[] args)
        {
            var newProgress = new DownloadProgress
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
                return await fileDownloader.DownloadFileWithResumeAsync(url, fileLocation);
            }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x27 || (ex.HResult & 0xFFFF) == 0x70)
            {
                // Disk Full, HRESULT: ‭-2147024784‬ == 0xFFFFFFFF80070070
                Logger.Error("AbstractDownloader:DownloadBinaryFile: {0}", ex);
                shellService.ShowError(ex, Resources.DiskFull);
                crawlerService.StopCommand.Execute(null);
                throw;
            }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x20)
            {
                // The process cannot access the file because it is being used by another process.", HRESULT: -2147024864 == 0xFFFFFFFF80070020
                return true;
            }
            catch (WebException webException) when ((webException.Response != null))
            {
                var webRespStatusCode = (int)((HttpWebResponse)webException.Response).StatusCode;
                if (webRespStatusCode >= 400 && webRespStatusCode < 600
                ) // removes inaccessible files: http status codes 400 to 599
                {
                    try
                    {
                        File.Delete(fileLocation);
                    } // could be open again in a different thread
                    catch
                    {
                    }
                }

                return false;
            }
            catch (TimeoutException timeoutException)
            {
                Logger.Error("AbstractDownloader:DownloadBinaryFile {0}", timeoutException);
                shellService.ShowError(timeoutException, Resources.TimeoutReached, Resources.Downloading, blog.Name);
                throw;
            }
        }

        protected virtual async Task<bool> DownloadBinaryFile(string fileLocation, string fileLocationUrlList, string url)
        {
            if (!blog.DownloadUrlList)
                return await DownloadBinaryFile(fileLocation, url);

            return AppendToTextFile(fileLocationUrlList, url);
        }

        protected virtual bool AppendToTextFile(string fileLocation, string text)
        {
            try
            {
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
                Logger.Error("Downloader:AppendToTextFile: {0}", ex);
                shellService.ShowError(ex, Resources.DiskFull);
                crawlerService.StopCommand.Execute(null);
                return false;
            }
            catch
            {
                return false;
            }
        }

        public virtual async Task<bool> DownloadBlogAsync()
        {
            concurrentConnectionsSemaphore =
                new SemaphoreSlim(shellService.Settings.ConcurrentConnections / crawlerService.ActiveItems.Count);
            concurrentVideoConnectionsSemaphore =
                new SemaphoreSlim(shellService.Settings.ConcurrentVideoConnections / crawlerService.ActiveItems.Count);
            var trackedTasks = new List<Task>();
            var completeDownload = true;

            blog.CreateDataFolder();

            foreach (TumblrPost downloadItem in postQueue.GetConsumingEnumerable())
            {
                if (downloadItem.GetType() == typeof(VideoPost))
                    await concurrentVideoConnectionsSemaphore.WaitAsync();
                await concurrentConnectionsSemaphore.WaitAsync();

                if (CheckifShouldStop())
                    break;

                CheckIfShouldPause();

                trackedTasks.Add(DownloadPost(downloadItem));
            }

            // TODO: Is this even right?
            try
            {
                await Task.WhenAll(trackedTasks);
            }
            catch
            {
                completeDownload = false;
            }

            blog.LastDownloadedPhoto = null;
            blog.LastDownloadedVideo = null;

            files.Save();

            return completeDownload;
        }

        private async Task DownloadPost(TumblrPost downloadItem)
        {
            try
            {
                await DownloadPostAsync(downloadItem);
            }
            catch
            {
            }
            finally
            {
                concurrentConnectionsSemaphore.Release();
                if (downloadItem.GetType() == typeof(VideoPost))
                    concurrentVideoConnectionsSemaphore.Release();
            }
        }

        private async Task DownloadPostAsync(TumblrPost downloadItem)
        {
            // TODO: Refactor, should be polymorphism
            if (downloadItem.PostType == PostType.Binary)
                await DownloadBinaryPost(downloadItem);
            else
                DownloadTextPost(downloadItem);
        }

        protected virtual async Task<bool> DownloadBinaryPost(TumblrPost downloadItem)
        {
            string url = Url(downloadItem);
            if (CheckIfFileExistsInDB(url))
            {
                string fileName = FileName(downloadItem);
                UpdateProgressQueueInformation(Resources.ProgressSkipFile, fileName);
            }
            else
            {
                string blogDownloadLocation = blog.DownloadLocation();
                string fileName = FileName(downloadItem);
                string fileLocation = FileLocation(blogDownloadLocation, fileName);
                string fileLocationUrlList = FileLocationLocalized(blogDownloadLocation, downloadItem.TextFileLocation);
                DateTime postDate = PostDate(downloadItem);
                UpdateProgressQueueInformation(Resources.ProgressDownloadImage, fileName);
                if (!await DownloadBinaryFile(fileLocation, fileLocationUrlList, url))
                    return false;
                SetFileDate(fileLocation, postDate);
                UpdateBlogDB(downloadItem.DbType, fileName);

                //TODO: Refactor
                if (!shellService.Settings.EnablePreview)
                    return true;

                if (suffixes.Any(suffix => fileName.EndsWith(suffix)))
                    blog.LastDownloadedPhoto = Path.GetFullPath(fileLocation);
                else
                    blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);

                return true;
            }

            return true;
        }

        private bool CheckIfFileExistsInDB(string url)
        {
            if (shellService.Settings.LoadAllDatabases)
                return managerService.CheckIfFileExistsInDB(url);

            return files.CheckIfFileExistsInDB(url) || blog.CheckIfBlogShouldCheckDirectory(GetCoreImageUrl(url));
        }

        private void DownloadTextPost(TumblrPost downloadItem)
        {
            string postId = PostId(downloadItem);
            if (CheckIfFileExistsInDB(postId))
            {
                UpdateProgressQueueInformation(Resources.ProgressSkipFile, postId);
            }
            else
            {
                string blogDownloadLocation = blog.DownloadLocation();
                string url = Url(downloadItem);
                string fileLocation = FileLocationLocalized(blogDownloadLocation, downloadItem.TextFileLocation);
                UpdateProgressQueueInformation(Resources.ProgressDownloadImage, postId);
                if (AppendToTextFile(fileLocation, url))
                    UpdateBlogDB(downloadItem.DbType, postId);
            }
        }

        private void UpdateBlogDB(string postType, string fileName)
        {
            blog.UpdatePostCount(postType);
            blog.UpdateProgress();
            files.AddFileToDb(fileName);
        }

        protected void SetFileDate(string fileLocation, DateTime postDate)
        {
            if (blog.DownloadUrlList)
                return;

            File.SetLastWriteTime(fileLocation, postDate);
        }

        protected static string Url(TumblrPost downloadItem)
        {
            return downloadItem.Url;
        }

        private static string FileName(TumblrPost downloadItem)
        {
            return downloadItem.Url.Split('/').Last();
        }

        protected static string FileLocation(string blogDownloadLocation, string fileName)
        {
            return Path.Combine(blogDownloadLocation, fileName);
        }

        protected static string FileLocationLocalized(string blogDownloadLocation, string fileName)
        {
            return Path.Combine(blogDownloadLocation, string.Format(CultureInfo.CurrentCulture, fileName));
        }

        private static string PostId(TumblrPost downloadItem)
        {
            return downloadItem.Id;
        }

        protected static DateTime PostDate(TumblrPost downloadItem)
        {
            if (string.IsNullOrEmpty(downloadItem.Date))
                return DateTime.Now;

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            DateTime postDate = epoch.AddSeconds(Convert.ToDouble(downloadItem.Date)).ToLocalTime();
            return postDate;
        }

        protected bool CheckifShouldStop()
        {
            return ct.IsCancellationRequested;
        }

        protected void CheckIfShouldPause()
        {
            if (pt.IsPaused)
                pt.WaitWhilePausedWithResponseAsyc().Wait();
        }
    }
}
