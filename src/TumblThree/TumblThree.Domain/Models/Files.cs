using System;
using System.Collections.Generic;
using System.IO;
using System.Waf.Foundation;

namespace TumblThree.Domain.Models
{
    public class Files : Model, IFiles
    {
        private string name;
        private string location;
        private BlogTypes blogType;
        private IList<string> links;

        public Files() : this(String.Empty, String.Empty, BlogTypes.tumblr)
        {
        }

        public Files(string name, string location, BlogTypes blogType)
        {
            this.name = name;
            this.location = location;
            this.BlogType = blogType;
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
                    File.WriteAllText(newIndex, JsonFormatter.FormatOutput(jsJson.Serialize(this)));
                    File.Replace(newIndex, currentIndex, backupIndex, true);
                    File.Delete(backupIndex);
                }
                else
                {
                    System.Web.Script.Serialization.JavaScriptSerializer jsJson = new System.Web.Script.Serialization.JavaScriptSerializer();
                    jsJson.MaxJsonLength = 2147483644;
                    File.WriteAllText(currentIndex, JsonFormatter.FormatOutput(jsJson.Serialize(this)));
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Files:Save: {0}", ex);
                throw;
            }
        }
    }
}
