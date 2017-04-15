using System;
using System.Collections.Generic;
using System.IO;
using System.Waf.Foundation;

namespace TumblThree.Domain.Models
{
    [Serializable]
    public class Files : Model, IFiles
    {
        private BlogTypes blogType;
        private IList<string> links;
        private string location;
        private string name;

        public Files() : this(string.Empty, string.Empty, BlogTypes.tumblr)
        {
        }

        protected Files(string name, string location, BlogTypes blogType)
        {
            this.name = name;
            this.location = location;
            BlogType = blogType;
            links = new List<string>();
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

        public IFiles Load(string fileLocation)
        {
            try
            {
                string json = File.ReadAllText(fileLocation);
                var jsJson =
                    new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = 2147483644 };
                var file = jsJson.Deserialize<Files>(json);
                file.Location = Path.Combine((Directory.GetParent(fileLocation).FullName));
                return file;
            }
            catch (ArgumentException ex)
            {
                ex.Data.Add("Filename", fileLocation);
                throw;
            }
        }

        public bool Save()
        {
            string currentIndex = Path.Combine(location, Name + "_files." + BlogType);
            string newIndex = Path.Combine(location, Name + "_files." + BlogType + ".new");
            string backupIndex = Path.Combine(location, Name + "_files." + BlogType + ".bak");

            try
            {
                if (File.Exists(currentIndex))
                {
                    var jsJson =
                        new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = 2147483644 };
                    File.WriteAllText(newIndex, JsonFormatter.FormatOutput(jsJson.Serialize(this)));
                    File.Replace(newIndex, currentIndex, backupIndex, true);
                    File.Delete(backupIndex);
                }
                else
                {
                    var jsJson =
                        new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = 2147483644 };
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
