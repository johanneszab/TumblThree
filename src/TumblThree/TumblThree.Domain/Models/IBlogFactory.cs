namespace TumblThree.Domain.Models
{
    public interface IBlogFactory
    {
        IBlog GetBlog(string blogUrl, string path);
    }
}
