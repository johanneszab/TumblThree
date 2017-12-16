using System.Threading.Tasks;

namespace TumblThree.Applications.Crawler
{
    interface ITumblrBlogDetector
    {
        Task<bool> IsTumblrBlog(string url);
    }
}