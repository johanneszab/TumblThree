using System.Threading.Tasks;

namespace TumblThree.Applications.Crawler
{
    interface ITumblrBlogDetector
    {
        Task<bool> IsHiddenTumblrBlog(string url);

        Task<bool> IsPasswordProtectedTumblrBlog(string url);

        Task<bool> IsTumblrBlog(string url);
    }
}