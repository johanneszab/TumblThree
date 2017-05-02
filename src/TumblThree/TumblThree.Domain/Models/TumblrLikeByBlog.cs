using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class TumblrLikeByBlog : Blog
    {
        public TumblrLikeByBlog(string url, string location, BlogTypes blogType) : base(url, location, blogType)
        {
        }

        protected override string ExtractName()
        {
            return Url.Split('/')[5];
        }

        protected override string ExtractUrl()
        {
            if (Url.StartsWith("http://"))
                Url = Url.Insert(4, "s");
            int blogNameLength = Url.Split('/')[5].Length;
            var urlLength = 32;
            return Url.Substring(0, blogNameLength + urlLength);
        }
    }
}
