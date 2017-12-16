using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class TumblrTagSearchBlog : Blog
    {
        public static Blog Create(string url, string location)
        {
            var blog = new TumblrTagSearchBlog()
            {
                Url = ExtractUrl(url),
                Name = ExtractName(url),
                BlogType = BlogTypes.tumblrtagsearch,
                Location = location,
                Online = true,
                Version = "3",
                DateAdded = DateTime.Now,
                links = new List<string>()
            };

            Directory.CreateDirectory(location);
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(location).FullName, blog.Name));

            blog.ChildId = Path.Combine(location, blog.Name + "_files." + blog.BlogType);
            if (!File.Exists(blog.ChildId))
            {
                IFiles files = new TumblrTagSearchBlogFiles(blog.Name, blog.Location);
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
