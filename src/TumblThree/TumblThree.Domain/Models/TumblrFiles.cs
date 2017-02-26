using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Waf.Foundation;

namespace TumblThree.Domain.Models
{
    [Serializable]
    public class TumblrFiles : Model
    {
        private string name;
        private string parentId;
        private string version;
        private IList<string> links;

        public TumblrFiles()
        {
            this.version = "1";
            this.links = new List<string>();
        }

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public string ParentId
        {
            get { return parentId; }
            set { SetProperty(ref parentId, value); }
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
    }
}
