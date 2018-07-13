using System;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading.Tasks;

using TumblThree.Applications.Extensions;
using TumblThree.Applications.Services;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ITumblrBlogDetector))]
    class TumblrBlogDetector : ITumblrBlogDetector
    {
        private readonly IWebRequestFactory webRequestFactory;
        private readonly IShellService shellService;
        protected readonly ISharedCookieService cookieService;

        [ImportingConstructor]
        public TumblrBlogDetector(IShellService shellService, ISharedCookieService cookieService, IWebRequestFactory webRequestFactory)
        {
            this.webRequestFactory = webRequestFactory;
            this.cookieService = cookieService;
            this.shellService = shellService;
        }

        public async Task<bool> IsTumblrBlog(string url)
        {
            string location = await GetUrlRedirection(url);
            if (location.Contains("login_required"))
                return false;
            return true;
        }

        public async Task<bool> IsHiddenTumblrBlog(string url)
        {
            string location = await GetUrlRedirection(url);
            if (location.Contains("login_required") || location.Contains("dashboard/blog/"))
                return true;
            return false;
        }

        public async Task<bool> IsPasswordProtectedTumblrBlog(string url)
        {
            string location = await GetUrlRedirection(url);
            if (location.Contains("blog_auth"))
                return true;
            return false;
        }

        private async Task<string> GetUrlRedirection(string url)
        {
            HttpWebRequest request = webRequestFactory.CreateGetReqeust(url);
            cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
            string location;
            using (var response = await request.GetResponseAsync().TimeoutAfter(shellService.Settings.TimeOut) as HttpWebResponse)
            {
                location = response.ResponseUri.ToString();
            }
            return location;
        }
    }
}
