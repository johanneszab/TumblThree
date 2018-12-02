using System.Threading.Tasks;

namespace TumblThree.Applications.Crawler
{
    interface ITumblrBlogDetector
    {
        Task<bool> IsPasswordProtectedTumblrBlogAsync(string url);

        Task<bool> IsTumblrBlogAsync(string url);
    }
}
