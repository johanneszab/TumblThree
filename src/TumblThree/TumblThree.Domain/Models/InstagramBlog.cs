using System;

namespace TumblThree.Domain.Models
{
    [Serializable]
    public class InstagramBlog : Blog
    {
        public InstagramBlog(string url, string location, BlogTypes type) : base(url, location, type)
        {
        }
    }
}
