using System;
using System.IO;
using System.Runtime.Serialization;

using TumblThree.Domain.Models.Files;

namespace TumblThree.Domain.Models.Blogs
{
    [DataContract]
    public class TumblrBlog : Blog
    {
        public static Blog Create(string url, string location)
        {
            var blog = new TumblrBlog()
            {
                Url = ExtractUrl(url),
                Name = ExtractName(url),
                BlogType = Models.BlogTypes.tumblr,
                OriginalBlogType = Models.BlogTypes.tumblr,
                Location = location,
                Online = true,
                Version = "4",
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
    }
}
