using System;
using System.Waf.Foundation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace TumblThree.Domain.Models
{
    [Serializable]
    public abstract class Blog : Model, IBlog
    {

        private string name;
        private string url;
        private string location;
        private BlogTypes blogType;
        private int downloadedImages;
        private int totalCount;
        private int rating;
        private DateTime dateAdded;
        private DateTime lastCompleteCrawl;
        private bool online;
        private bool dirty;
        private string notes;
        private IList<string> links;
        private Exception loadError;

        protected Blog()
        {
        }

        protected Blog(string url, string location, BlogTypes blogType)
        {
            this.url = url;
            this.url = ExtractUrl();
            this.name = ExtractSubDomain();
            this.location = location;
            this.blogType = blogType;
            this.downloadedImages = 0;
            this.totalCount = 0;
            this.rating = 0;
            this.dateAdded = System.DateTime.Now;
            this.lastCompleteCrawl = System.DateTime.MinValue;
            this.online = false;
            this.dirty = false;
            this.notes = String.Empty;
            this.links = new ObservableCollection<string>();
        }

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public string Url
        {
            get { return url; }
            set { SetProperty(ref url, value); }
        }

        public string Location
        {
            get { return location; }
            set { SetProperty(ref location, value); }
        }

        public BlogTypes BlogType
        {
            get { return blogType; }
            set { SetProperty(ref blogType, value); }
        }

        public int DownloadedImages
        {
            get { return downloadedImages; }
            set { SetProperty(ref downloadedImages, value); }
        }

        public int TotalCount
        {
            get { return totalCount; }
            set { SetProperty(ref totalCount, value); }
        }

        public int Rating
        {
            get { return rating; }
            set { SetProperty(ref rating, value); Dirty = true; }
        }

        public DateTime DateAdded
        {
            get { return dateAdded; }
            set { SetProperty(ref dateAdded, value); }
        }

        public DateTime LastCompleteCrawl
        {
            get { return lastCompleteCrawl; }
            set { SetProperty(ref lastCompleteCrawl, value); }
        }

        public bool Online
        {
            get { return online; }
            set { SetProperty(ref online, value); }
        }

        public bool Dirty
        {
            get { return dirty; }
            set { SetProperty(ref dirty, value); }
        }

        public string Notes
        {
            get { return notes; }
            set { SetProperty(ref notes, value); Dirty = true; }
        }

        public Exception LoadError
        {
            get { return loadError; }
            set { SetProperty(ref loadError, value); }
        }

        public IList<string> Links
        {
            get { return links; }
            set { SetProperty(ref links, value); }
        }

        private void SaveBlog()
        {
            string currentIndex = Path.Combine(location, this.Name + "." + this.BlogType);
            string newIndex = Path.Combine(location, this.Name + "." + this.BlogType + ".new");
            string backupIndex = Path.Combine(location, this.Name + "." + this.BlogType + ".bak");

            if (File.Exists(currentIndex))
            {
                System.Web.Script.Serialization.JavaScriptSerializer jsJson = new System.Web.Script.Serialization.JavaScriptSerializer();
                jsJson.MaxJsonLength = 2147483644;
                File.WriteAllText(newIndex, jsJson.Serialize(this));
                File.Replace(newIndex, currentIndex, backupIndex, true);
                File.Delete(backupIndex);
            }
            else
            {
                System.Web.Script.Serialization.JavaScriptSerializer jsJson = new System.Web.Script.Serialization.JavaScriptSerializer();
                jsJson.MaxJsonLength = 2147483644;
                File.WriteAllText(currentIndex, jsJson.Serialize(this));
            }
        }

        public bool Save()
        {
            try
            {
                this.Dirty = false;
                SaveBlog();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Blog:Save: {0}", ex);
                throw;
            }
        }

        protected virtual string ExtractSubDomain()
        {
            string[] source = this.Url.Split(new char[] { '.' });
            if ((source.Count<string>() >= 3) && source[0].StartsWith("http://", true, null))
            {
                return source[0].Replace("http://", string.Empty);
            }
            else if ((source.Count<string>() >= 3) && source[0].StartsWith("https://", true, null))
            {
                return source[0].Replace("https://", string.Empty);
            }
            return null;
        }

        protected virtual string ExtractUrl()
        {
            return ("https://" + ExtractSubDomain() + ".tumblr.com/");
        }
    }
}
