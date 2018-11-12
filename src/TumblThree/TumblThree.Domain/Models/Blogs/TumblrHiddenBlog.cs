using System;
using System.IO;
using System.Runtime.Serialization;

using TumblThree.Domain.Models.Files;

namespace TumblThree.Domain.Models.Blogs
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

        protected new static string ExtractName(string url) => url.Split('/')[5];

        protected new static string ExtractUrl(string url) => "https://" + ExtractName(url) + ".tumblr.com/";
    }
}
