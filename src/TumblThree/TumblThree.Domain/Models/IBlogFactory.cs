namespace TumblThree.Domain.Models
{
    public interface IBlogFactory
    {
        bool IsValidTumblrBlogUrl(string blogUrl);

        IBlog GetBlog(string blogUrl, string path);
    }
}
