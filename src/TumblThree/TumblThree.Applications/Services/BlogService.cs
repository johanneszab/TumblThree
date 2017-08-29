using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Services
{
    public class BlogService : IBlogService
    {
        protected readonly IBlog blog;
        protected readonly IFiles files;
        protected readonly object lockObjectProgress = new object();
        protected readonly object lockObjectPostCount = new object();
        protected readonly object lockObjectDb = new object();
        protected readonly object lockObjectDirectory = new object();

        public BlogService(IBlog blog, IFiles files)
        {
            this.blog = blog;
            this.files = files;
        }

        public void UpdateBlogProgress()
        {
            lock (lockObjectProgress)
            {
                blog.DownloadedImages++;
                blog.Progress = (int)((double)blog.DownloadedImages / (double)blog.TotalCount * 100);
            }
        }

        public void UpdateBlogPostCount(string propertyName)
        {
            lock (lockObjectPostCount)
            {
                PropertyInfo property = typeof(IBlog).GetProperty(propertyName);
                var postCounter = (int)property.GetValue(blog);
                postCounter++;
                property.SetValue(blog, postCounter, null);
            }

        }

        public void UpdateBlogDB(string fileName)
        {
            lock (lockObjectProgress)
            {
                files.Links.Add(fileName);
            }
        }

        public bool CreateDataFolder()
        {
            if (string.IsNullOrEmpty(blog.Name))
            {
                return false;
            }

            string blogPath = blog.DownloadLocation();

            if (!Directory.Exists(blogPath))
            {
                Directory.CreateDirectory(blogPath);
                return true;
            }
            return true;
        }

        public virtual bool CheckIfFileExistsInDB(string url)
        {
            string fileName = url.Split('/').Last();
            Monitor.Enter(lockObjectDb);
            if (files.Links.Contains(fileName))
            {
                Monitor.Exit(lockObjectDb);
                return true;
            }
            Monitor.Exit(lockObjectDb);
            return false;
        }

        public virtual bool CheckIfBlogShouldCheckDirectory(string url)
        {
            if (blog.CheckDirectoryForFiles)
            {
                return CheckIfFileExistsInDirectory(url);
            }
            return false;
        }

        public virtual bool CheckIfFileExistsInDirectory(string url)
        {
            string fileName = url.Split('/').Last();
            Monitor.Enter(lockObjectDirectory);
            string blogPath = blog.DownloadLocation();
            if (File.Exists(Path.Combine(blogPath, fileName)))
            {
                Monitor.Exit(lockObjectDirectory);
                return true;
            }
            Monitor.Exit(lockObjectDirectory);
            return false;
        }

        public virtual void SaveFiles()
        {
            files.Save();
        }
    }
}
