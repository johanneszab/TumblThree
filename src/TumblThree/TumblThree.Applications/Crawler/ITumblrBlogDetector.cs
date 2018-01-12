using System.Threading.Tasks;

namespace TumblThree.Applications.Crawler
{
	internal interface ITumblrBlogDetector
    {
        Task<bool> IsHiddenTumblrBlog(string url);

        Task<bool> IsTumblrBlog(string url);
    }
}