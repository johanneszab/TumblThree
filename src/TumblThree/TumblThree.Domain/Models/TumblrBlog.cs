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
        private string text;
        private uint progress;
        private string tags;

        public TumblrBlog()
        {
            this.description = null;
            this.text = null;
            this.progress = 0;
            this.tags = null;
        }

        public TumblrBlog(string url)
        {
            this.description = null;
            this.Url = url;
            this.text = null;
            this.progress = 0;
            this.tags = null;
        }

        public string Description
        {
            get { return description; }
            set { SetProperty(ref description, value); }
        }

        public string Text
        {
            get { return text; }
            set { SetProperty(ref text, value); }
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
