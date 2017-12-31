using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class TumblrHiddenBlog : Blog
    {
        public static Blog Create(string url, string location)
        {
            var blog = new TumblrBlog()
            {
                Url = ExtractUrl(url),
                Name = ExtractName(url),
                BlogType = BlogTypes.tumblr,
                Location = location,
                Online = true,
                Version = "3",
                DateAdded = DateTime.Now,
            };

            Directory.CreateDirectory(location);
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(location).FullName, blog.Name));

            blog.ChildId = Path.Combine(location, blog.Name + "_files." + blog.BlogType);
            if (!File.Exists(blog.ChildId))
            {
                IFiles files = new TumblrBlogFiles(blog.Name, blog.Location);
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
            return "https://" + ExtractName(url) + ".tumblr.com/";
        }
    }
}
