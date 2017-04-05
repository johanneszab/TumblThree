using System;

namespace TumblThree.Domain.Models
{
    [Serializable]
    public class InstagramFiles : Files
    {
        private string version;

        public InstagramFiles(string name, string location, BlogTypes blogType) : base(name, location, blogType)
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
