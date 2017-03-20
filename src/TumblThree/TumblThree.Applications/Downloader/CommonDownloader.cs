using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
    /// <summary>
    /// Object responsible of downloading the actual files and writing
    /// to disk.
    /// </summary>
    /// <remarks>
    /// Longer comments can be associated with a type or member through
    /// the remarks tag.
    /// </remarks>
    public class CommonDownloader : ICommonDownloader
    {

        private readonly IShellService shellService;

        public CommonDownloader(IShellService shellService)
        {
            this.shellService = shellService;
        }

        protected virtual String RequestData(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                if (!String.IsNullOrEmpty(shellService.Settings.ProxyHost))
                {
                    request.Proxy = new WebProxy(shellService.Settings.ProxyHost, Int32.Parse(shellService.Settings.ProxyPort));
                }
                else
                {
                    request.Proxy = null;
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

                int bandwidth = 2000000;
                if (shellService.Settings.LimitScanBandwidth)
                {
                    bandwidth = shellService.Settings.Bandwidth;
                }

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
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


        protected virtual string ExtractUrl(string url)
        {
            throw new NotImplementedException();
        }


        public virtual Task<bool> IsBlogOnline(string url)
        {
            return Task<bool>.Factory.StartNew(() =>
            {
                string request = RequestData(url);

                if (request != null)
                    return true;
                else
                    return false;
            },
            TaskCreationOptions.LongRunning);
        }


        protected virtual string ExtractSubDomain(string url)
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


        public static bool CreateDataFolder(string name, string location)
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


        protected virtual bool Download(TumblrFiles blog, string fileLocation, string url, IProgress<DataModels.DownloadProgress> progress, object lockObject, bool locked, ref int counter, ref int totalCounter)
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
                    throw;
                }
                catch
                {
                    return false;
                }
            }
        }

        protected virtual bool Download(TumblrFiles blog, string fileLocation, string postId, string text, IProgress<DataModels.DownloadProgress> progress, object lockObject, bool locked, ref int counter, ref int totalCounter)
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
                    throw;
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

        protected virtual void UpdateProgress(TumblrBlog blog, TumblrFiles files, string fileName, object lockObjectProgress, ref int totalCounter)
        {
            lock (lockObjectProgress)
            {
                files.Links.Add(fileName);
                // the following could be moved out of the lock?
                blog.DownloadedImages = totalCounter;
                blog.Progress = (int)((double)totalCounter / (double)blog.TotalCount * 100);
            }
        }
    }
}
