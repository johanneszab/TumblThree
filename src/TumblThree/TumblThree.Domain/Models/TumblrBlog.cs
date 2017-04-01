using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [Serializable]
    public class TumblrBlog : Blog
    {
        private string version;
        private string description;
        private string title;
        private ulong lastId;
        private string tags;
        private bool skipGif;
        private bool forceSize;
        private bool forceRescan;

        public TumblrBlog()
        {
            /// <summary>
            /// DON'T use. Only for Mockup
            /// </summary>
        }

        public TumblrBlog(string url, string location, BlogTypes type) : base(url, location, type)
        {
            this.version = "3";
            this.description = String.Empty;
            this.title = String.Empty;
            this.lastId = 0;
            this.tags = String.Empty;
            this.skipGif = false;
            this.forceSize = false;
            this.forceRescan = false;

            Directory.CreateDirectory(Location);
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Location).FullName, Name));

            if (!File.Exists(ChildId))
            {
                Files files = new TumblrFiles(Name, Location, BlogType);

                files.Save();
                files = null;
            }
        }

        public string Version
        {
            get { return version; }
            set { SetProperty(ref version, value); }
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

        public ulong LastId
        {
            get { return lastId; }
            set { SetProperty(ref lastId, value); }
        }

        public string Tags
        {
            get { return tags; }
            set { SetProperty(ref tags, value); Dirty = true; }
        }

        public bool SkipGif
        {
            get { return skipGif; }
            set { SetProperty(ref skipGif, value); Dirty = true; }
        }

        public bool ForceSize
        {
            get { return forceSize; }
            set { SetProperty(ref forceSize, value); Dirty = true; }
        }

        public bool ForceRescan
        {
            get { return forceRescan; }
            set { SetProperty(ref forceRescan, value); Dirty = true; }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
        }

        public bool Update()
        {
            if (!this.Version.Equals("3"))
            {
                BlogType = BlogTypes.tumblr;
                Version = "3";
                Dirty = true;
            }
            return true;
        }
    }
}
