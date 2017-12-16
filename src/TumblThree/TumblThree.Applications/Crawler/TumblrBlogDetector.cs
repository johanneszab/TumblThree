using System.ComponentModel.Composition;
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
    }
}
