using System;
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
        private readonly IShellService shellService;
        protected readonly object lockObjectDb;
        protected readonly object lockObjectDirectory;
        protected readonly object lockObjectDownload;


        public Downloader(IShellService shellService): this(shellService, null)
        {
        }

        public Downloader(IShellService shellService, IBlog blog)
        {
            this.shellService = shellService;
            this.blog = blog;
            this.lockObjectDb = new object();
            this.lockObjectDirectory = new object();
            this.lockObjectDownload = new object();
            SetUp();
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

        public virtual async Task IsBlogOnline()
        {
            string request = await RequestDataAsync(blog.Url);

            if (request != null)
                blog.Online = true;
            else
                blog.Online = false;
        }

        protected virtual async void SetUp()
        {
            CreateDataFolder();
            await IsBlogOnline();
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

        protected virtual async Task<bool> AppendToTextFile(string fileLocation, string text)
        {
            try
            {
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
    }
}
