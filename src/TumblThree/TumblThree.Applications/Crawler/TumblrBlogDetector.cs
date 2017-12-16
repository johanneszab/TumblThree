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

        [ImportingConstructor]
        public TumblrBlogDetector(IShellService shellService, IWebRequestFactory webRequestFactory)
        {
            this.webRequestFactory = webRequestFactory;
            this.shellService = shellService;
        }

        public async Task<bool> IsTumblrBlog(string url)
        {
            string location = await GetUrlRedirection(url).TimeoutAfter(shellService.Settings.TimeOut); ;
            if (location.Contains("login_required"))
                return false;
            return true;
        }

        public async Task<bool> IsHiddenTumblrBlog(string url)
        {
            string location = await GetUrlRedirection(url).TimeoutAfter(shellService.Settings.TimeOut); ;
            if (location.Contains("login_required"))
                return true;
            return false;
        }

        private async Task<string> GetUrlRedirection(string url)
        {
            HttpWebRequest request = webRequestFactory.CreateGetReqeust(url);
            request.Method = "HEAD";
            string location;
            using (var response = await request.GetResponseAsync() as HttpWebResponse)
            {
                location = response.ResponseUri.ToString();
            }
            return location;
        }
    }
}
