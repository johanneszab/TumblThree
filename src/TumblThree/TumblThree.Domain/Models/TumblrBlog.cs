using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TumblThree.Domain.Models
{
    [Serializable]
    public class TumblrBlog : Blog
    {
        private string description;
        private string title;
        private uint progress;
        private string tags;

        public TumblrBlog()
        {
            this.description = null;
            this.title = null;
            this.progress = 0;
            this.tags = null;
        }

        public TumblrBlog(string url)
        {
            this.description = null;
            this.Url = url;
            this.title = null;
            this.progress = 0;
            this.tags = null;
        }

        public string Description
        {
            get { return description; }
            set { SetProperty(ref description, value); }
        }

        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        public uint Progress
        {
            get { return progress; }
            set { SetProperty(ref progress, value); }
        }

        public string Tags
        {
            get { return tags; }
            set { SetProperty(ref tags, value); }
        }

    }
}
