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
            return await webRequestFactory.RemotePageIsValid(url).TimeoutAfter(shellService.Settings.TimeOut);
        }

        public async Task<bool> IsPasswordProtectedTumblrBlog(string url)
        {
            string location = await GetUrlRedirection(url).TimeoutAfter(shellService.Settings.TimeOut); ;
            if (location.Contains("blog_auth"))
                return true;
            return false;
        }

        private async Task<string> GetUrlRedirection(string url)
        {
            HttpWebRequest request = webRequestFactory.CreateGetReqeust(url);
            request.Method = "GET";
            string location;
            using (var response = await request.GetResponseAsync() as HttpWebResponse)
            {
                location = response.ResponseUri.ToString();
            }
            return location;
        }
    }
}
