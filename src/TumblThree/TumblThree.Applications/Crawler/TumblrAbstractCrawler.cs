using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.Extensions;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
    public abstract class TumblrAbstractCrawler : AbstractCrawler
    {
        protected TumblrAbstractCrawler(IShellService shellService, ICrawlerService crawlerService,
            CancellationToken ct, IProgress<DownloadProgress> progress, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IPostQueue<TumblrPost> postQueue, IBlog blog) 
            : base(shellService, crawlerService, ct, progress, webRequestFactory, cookieService, postQueue, blog)
        {
        }

        protected override async Task<string> RequestDataAsync(string url, params string[] cookieHosts)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>(); 
            string username = blog.Name + ".tumblr.com";
            string password = blog.Password;
            string encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
            headers.Add("Authorization", "Basic " + encoded);
            return await RequestDataAsync(url, headers, cookieHosts);
        }

        protected async Task<string> GetRequestAsync(string url)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                HttpWebRequest request = webRequestFactory.CreateGetReqeust(url);
                cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
                cookieService.GetUriCookie(request.CookieContainer, new Uri("https://" + blog.Name.Replace("+", "-") + ".tumblr.com"));

                requestRegistration = ct.Register(() => request.Abort());
                return await webRequestFactory.ReadReqestToEnd(request).TimeoutAfter(shellService.Settings.TimeOut);
            }
            finally
            {
                requestRegistration.Dispose();
            }
        }

        protected async Task<string> UpdateTumblrKey(string url)
        {
            try
            {
                string document = await GetRequestAsync(url);
                return ExtractTumblrKey(document);
            }
            catch (TimeoutException timeoutException)
            {
                Logger.Error("TumblrHiddenCrawler:IsBlogOnlineAsync:WebException {0}", timeoutException);
                shellService.ShowError(timeoutException, Resources.TimeoutReached, Resources.OnlineChecking, blog.Name);
                return string.Empty;
            }
        }

        protected static string ExtractTumblrKey(string document)
        {
            return Regex.Match(document, "id=\"tumblr_form_key\" content=\"([\\S]*)\">").Groups[1].Value;
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
    }
}
