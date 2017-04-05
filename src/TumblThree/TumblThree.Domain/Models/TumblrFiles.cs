using System;

namespace TumblThree.Domain.Models
{
    [Serializable]
    public class TumblrFiles : Files
    {
        private string version;

        public TumblrFiles(string name, string location, BlogTypes blogType) : base(name, location, blogType)
        {
            version = "1";
        }

        public string Version
        {
            get { return version; }
            set { SetProperty(ref version, value); }
        }
    }
}
