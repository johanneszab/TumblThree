using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class TumblrSearchBlog : Blog
    {
        public static Blog Create(string url, string location)
        {
            TumblrSearchBlog blog = new TumblrSearchBlog()
            {
                Url = ExtractUrl(url),
                Name = ExtractName(url),
                BlogType = BlogTypes.tumblrsearch,
                Location = location,
                Online = true,
                Version = "3",
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

        protected new static string ExtractName(string url)
        {
            return url.Split('/')[4].Replace("-", "+");
        }

        protected new static string ExtractUrl(string url)
        {
            if (url.StartsWith("http://"))
            {
	            url = url.Insert(4, "s");
            }
	        int blogNameLength = url.Split('/')[4].Length;
            int urlLength = 30;
            return url.Substring(0, blogNameLength + urlLength);
        }
    }
}
