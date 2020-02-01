using System;
using System.IO;
using System.Runtime.Serialization;

using TumblThree.Domain.Models.Files;

namespace TumblThree.Domain.Models.Blogs
{
    [DataContract]
    public class TumblrSearchBlog : Blog
    {
        public static Blog Create(string url, string location)
        {
            var blog = new TumblrSearchBlog()
            {
                Url = ExtractUrl(url),
                Name = ExtractName(url),
                BlogType = BlogTypes.tumblrsearch,
                OriginalBlogType = BlogTypes.tumblrsearch,
                Location = location,
                Online = true,
                Version = "4",
                DateAdded = DateTime.Now,
                PageSize = 20,
            };

            Directory.CreateDirectory(location);
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(location).FullName, blog.Name));

            blog.ChildId = Path.Combine(location, blog.Name + "_files." + blog.BlogType);
            if (!File.Exists(blog.ChildId))
            {
                IFiles files = new TumblrSearchBlogFiles(blog.Name, blog.Location);
                files.Save();
                files = null;
            }

            return blog;
        }

        protected static new string ExtractName(string url) => url.Split('/')[4].Replace("-", "+");

        protected static new string ExtractUrl(string url)
        {
            if (url.StartsWith("http://"))
            {
                url = url.Insert(4, "s");
            }

            int blogNameLength = url.Split('/')[4].Length;
            var urlLength = 30;
            return url.Substring(0, blogNameLength + urlLength);
        }
    }
}
