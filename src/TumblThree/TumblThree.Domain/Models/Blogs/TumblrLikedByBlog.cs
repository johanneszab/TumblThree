using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class TumblrLikedByBlog : Blog
    {
        public static Blog Create(string url, string location)
        {
            var blog = new TumblrLikedByBlog()
            {
                Url = ExtractUrl(url),
                Name = ExtractName(url),
                BlogType = BlogTypes.tlb,
                Location = location,
                Version = "3",
                DateAdded = DateTime.Now,
                links = new List<string>()
            };

            Directory.CreateDirectory(location);
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(location).FullName, blog.Name));

            blog.ChildId = Path.Combine(location, blog.Name + "_files." + blog.BlogType);
            if (!File.Exists(blog.ChildId))
            {
                IFiles files = new TumblrLikedByBlogFiles(blog.Name, blog.Location);
                files.Save();
                files = null;
            }
            return blog;
        }

        protected static new string ExtractName(string url)
        {
            return url.Split('/')[5];
        }

        protected static new string ExtractUrl(string url)
        {
            if (url.StartsWith("http://"))
                url = url.Insert(4, "s");
            int blogNameLength = url.Split('/')[5].Length;
            var urlLength = 32;
            return url.Substring(0, blogNameLength + urlLength);
        }
    }
}
