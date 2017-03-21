using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Waf.Foundation;

namespace TumblThree.Domain.Models
{
    [Serializable]
    public class TumblrFiles : Model
    {
        private string name;
        private string location;
        private string version;
        private BlogTypes blogType;
        private IList<string> links;

        public TumblrFiles() : this(String.Empty, String.Empty, BlogTypes.none)
        {
        }

        public TumblrFiles(string name, string location, BlogTypes blogTypes)
        {
            this.name = name;
            this.location = location;
            this.BlogType = blogType;
            this.version = "1";
            this.links = new List<string>();
        }

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
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

        public string Version
        {
            get { return version; }
            set { SetProperty(ref version, value); }
        }

        public IList<string> Links
        {
            get { return links; }
            set { SetProperty(ref links, value); }
        }

        public bool Save()
        {
            string currentIndex = Path.Combine(location, this.Name + "_files." + this.BlogType);
            string newIndex = Path.Combine(location, this.Name + "_files." + this.BlogType + ".new");
            string backupIndex = Path.Combine(location, this.Name + "_files." + this.BlogType + ".bak");

            try
            {
                if (File.Exists(currentIndex))
                {
                    System.Web.Script.Serialization.JavaScriptSerializer jsJson = new System.Web.Script.Serialization.JavaScriptSerializer();
                    jsJson.MaxJsonLength = 2147483644;
                    File.WriteAllText(newIndex, jsJson.Serialize(this)); File.Replace(newIndex, currentIndex, backupIndex, true);
                    File.Delete(backupIndex);
                }
                else
                {
                    System.Web.Script.Serialization.JavaScriptSerializer jsJson = new System.Web.Script.Serialization.JavaScriptSerializer();
                    jsJson.MaxJsonLength = 2147483644;
                    File.WriteAllText(currentIndex, jsJson.Serialize(this));
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("TumblrFiles:Save: {0}", ex);
                throw;
            }
        }
    }
}
