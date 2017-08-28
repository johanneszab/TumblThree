using System;
using System.IO;
using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class TumblrTagSearchBlog : Blog
    {
        public static new Blog Create(string url, string location, BlogTypes blogType)
        {
            var blog = new TumblrTagSearchBlog()
            {
                Url = ExtractUrl(url),
                Name = ExtractName(url),
                BlogType = blogType,
                Location = location,
                Version = "3",
                DateAdded = DateTime.Now
            };

            Directory.CreateDirectory(location);
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(location).FullName, blog.Name));

            blog.ChildId = Path.Combine(location, blog.Name + "_files." + blogType);
            if (!File.Exists(blog.ChildId))
            {
                IFiles files = new Files(blog.Name, blog.Location, blog.BlogType);
                files.Save();
                files = null;
            }
            return blog;
        }

        protected static new string ExtractName(string url)
        {
            return url.Split('/')[4].Replace("-", "+");
        }

        protected static new string ExtractUrl(string url)
        {
            if (url.StartsWith("http://"))
                url = url.Insert(4, "s");
            int blogNameLength = url.Split('/')[4].Length;
            var urlLength = 30;
            return url.Substring(0, blogNameLength + urlLength);
        }
    }
}
