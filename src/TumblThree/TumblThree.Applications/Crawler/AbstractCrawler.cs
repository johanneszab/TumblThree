using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Extensions;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
    public abstract class AbstractCrawler
    {
        protected readonly IBlog blog;
        protected readonly IProgress<DownloadProgress> progress;
        protected readonly ISharedCookieService cookieService;
        protected readonly IWebRequestFactory webRequestFactory;
        protected readonly object lockObjectDb = new object();
        protected readonly object lockObjectDirectory = new object();
        protected readonly object lockObjectDownload = new object();
        protected readonly object lockObjectProgress = new object();
        protected readonly IShellService shellService;
        protected readonly CancellationToken ct;
        protected readonly IPostQueue<TumblrPost> postQueue;
        protected ConcurrentBag<TumblrPost> statisticsBag = new ConcurrentBag<TumblrPost>();
        protected List<string> tags = new List<string>();
        protected int numberOfPagesCrawled = 0;

        protected AbstractCrawler(IShellService shellService, CancellationToken ct, IProgress<DownloadProgress> progress, IWebRequestFactory webRequestFactory, ISharedCookieService cookieService, IPostQueue<TumblrPost> postQueue, IBlog blog)
        {
            this.shellService = shellService;
            this.webRequestFactory = webRequestFactory;
            this.cookieService = cookieService;
            this.postQueue = postQueue;
            this.blog = blog;
            this.ct = ct;
            this.progress = progress;
        }

        public virtual async Task UpdateMetaInformationAsync()
        {
            await Task.FromResult<object>(null);
        }

        public virtual async Task IsBlogOnlineAsync()
        {
            try
            {
                await RequestDataAsync(blog.Url);
                blog.Online = true;
            }
            catch (WebException webException)
            {
                Logger.Error("AbstractCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                shellService.ShowError(webException, Resources.BlogIsOffline, blog.Name);
                blog.Online = false;
            }
            catch (TimeoutException timeoutException)
            {
                Logger.Error("TumblrBlogCrawler:CheckIfLoggedIn:WebException {0}", timeoutException);
                shellService.ShowError(timeoutException, Resources.TimeoutReached, Resources.OnlineChecking, blog.Name);
                blog.Online = false;
            }
        }

        public void UpdateProgressQueueInformation(string format, params object[] args)
        {
            var newProgress = new DataModels.DownloadProgress
            {
                Progress = string.Format(CultureInfo.CurrentCulture, format, args)
            };
            progress.Report(newProgress);
        }

        protected virtual async Task<string> RequestDataAsync(string url, params string[] cookieHosts)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                HttpWebRequest request = webRequestFactory.CreateGetReqeust(url);
                foreach (string cookieHost in cookieHosts)
                {
                    cookieService.GetUriCookie(request.CookieContainer, new Uri(cookieHost));
                }
                string username = blog.Name + ".tumblr.com";
                string password = blog.Password;
                string encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
                request.Headers.Add("Authorization", "Basic " + encoded);
                //request.Credentials = new NetworkCredential(blog.Name + ".tumblr.com", blog.Password);
                requestRegistration = ct.Register(() => request.Abort());
                return await webRequestFactory.ReadReqestToEnd(request).TimeoutAfter(shellService.Settings.TimeOut);
            }
            finally
            {
                requestRegistration.Dispose();
            }
        }

        protected static string UrlEncode(IDictionary<string, string> parameters)
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, string> val in parameters)
            {
                sb.AppendFormat("{0}={1}&", val.Key, HttpUtility.UrlEncode(val.Value));
            }
            sb.Remove(sb.Length - 1, 1); // remove last '&'
            return sb.ToString();
        }

        protected virtual IEnumerable<int> GetPageNumbers()
        {
            if (string.IsNullOrEmpty(blog.DownloadPages))
            {
                return Enumerable.Range(0, shellService.Settings.ConcurrentScans);
            }
            return RangeToSequence(blog.DownloadPages);
        }

        protected static bool TestRange(int numberToCheck, int bottom, int top)
        {
            return (numberToCheck >= bottom && numberToCheck <= top);
        }

        protected static IEnumerable<int> RangeToSequence(string input)
        {
            string[] parts = input.Split(',');
            foreach (string part in parts)
            {
                if (!part.Contains('-'))
                {
                    yield return int.Parse(part);
                    continue;
                }
                string[] rangeParts = part.Split('-');
                int start = int.Parse(rangeParts[0]);
                int end = int.Parse(rangeParts[1]);

                while (start <= end)
                {
                    yield return start;
                    start++;
                }
            }
        }

        protected void AddToDownloadList(TumblrPost addToList)
        {
            postQueue.Add(addToList);
            statisticsBag.Add(addToList);
        }

        public T ConvertJsonToClass<T>(string json) where T : new()
        {
            try
            {
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                return serializer.Deserialize<T>(json);
            }
            catch (InvalidOperationException invalidOperationException)
            {
                Logger.Error("AbstractCrawler:ConvertJsonToClass<T>: {0}", "Could not parse data");
                shellService.ShowError(invalidOperationException, Resources.PostNotParsable, blog.Name);
                return new T();
            }
        }

        protected string ImageSize()
        {
            if (shellService.Settings.ImageSize == "raw")
                return "1280";
            return shellService.Settings.ImageSize;
        }

        protected string ResizeTumblrImageUrl(string imageUrl)
        {
            var sb = new StringBuilder(imageUrl);
            return sb
                .Replace("_raw", "_" + ImageSize())
                .Replace("_1280", "_" + ImageSize())
                .Replace("_540", "_" + ImageSize())
                .Replace("_500", "_" + ImageSize())
                .Replace("_400", "_" + ImageSize())
                .Replace("_250", "_" + ImageSize())
                .Replace("_100", "_" + ImageSize())
                .Replace("_75sq", "_" + ImageSize())
                .ToString();
        }

        /// <returns>
        ///     Return the url without the size and type suffix (e.g.
        ///     https://68.media.tumblr.com/51a99943f4aa7068b6fd9a6b36e4961b/tumblr_mnj6m9Huml1qat3lvo1).
        /// </returns>
        protected string GetCoreImageUrl(string url)
        {
            // return url.Split('_')[0] + "_" + url.Split('_')[1];
            return url;
        }

        protected ulong GetLastPostId()
        {
            ulong lastId = blog.LastId;
            if (blog.ForceRescan)
            {
                return 0;
            }
            if (!string.IsNullOrEmpty(blog.DownloadPages))
            {
                return 0;
            }
            return lastId;
        }

        protected void UpdateBlogStats()
        {
            blog.TotalCount = statisticsBag.Count;
            blog.Photos = statisticsBag.Count(url => url.GetType() == typeof(PhotoPost));
            blog.Videos = statisticsBag.Count(url => url.GetType() == typeof(VideoPost));
            blog.Audios = statisticsBag.Count(url => url.GetType() == typeof(AudioPost));
            blog.Texts = statisticsBag.Count(url => url.GetType() == typeof(TextPost));
            blog.Answers = statisticsBag.Count(url => url.GetType() == typeof(AnswerPost));
            blog.Conversations = statisticsBag.Count(url => url.GetType() == typeof(ConversationPost));
            blog.Quotes = statisticsBag.Count(url => url.GetType() == typeof(QuotePost));
            blog.NumberOfLinks = statisticsBag.Count(url => url.GetType() == typeof(LinkPost));
            blog.PhotoMetas = statisticsBag.Count(url => url.GetType() == typeof(PhotoMetaPost));
            blog.VideoMetas = statisticsBag.Count(url => url.GetType() == typeof(VideoMetaPost));
            blog.AudioMetas = statisticsBag.Count(url => url.GetType() == typeof(AudioMetaPost));
        }

        protected int DetermineDuplicates<T>()
        {
            return statisticsBag.Where(url => url.GetType() == typeof(T))
                                .GroupBy(url => url.Url)
                                .Where(g => g.Count() > 1)
                                .Sum(g => g.Count() - 1);
        }

        protected void CleanCollectedBlogStatistics()
        {
            statisticsBag = null;
        }
    }
}
