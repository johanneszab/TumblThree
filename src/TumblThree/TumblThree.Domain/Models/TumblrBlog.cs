using System;
using System.IO;
using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [Serializable]
    public class TumblrBlog : Blog
    {
        private string description;
        private bool forceRescan;
        private bool forceSize;
        private ulong lastId;
        private bool skipGif;
        private string tags;
        private string title;
        private string version;

        /// <summary>
        ///     DON'T use. Only for Mockup
        /// </summary>
        public TumblrBlog()
        {
        }

        public TumblrBlog(string url, string location, BlogTypes type) : base(url, location, type)
        {
            version = "3";
            description = string.Empty;
            title = string.Empty;
            lastId = 0;
            tags = string.Empty;
            skipGif = false;
            forceSize = false;
            forceRescan = false;

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
            set
            {
                SetProperty(ref tags, value);
                Dirty = true;
            }
        }

        public bool SkipGif
        {
            get { return skipGif; }
            set
            {
                SetProperty(ref skipGif, value);
                Dirty = true;
            }
        }

        public bool ForceSize
        {
            get { return forceSize; }
            set
            {
                SetProperty(ref forceSize, value);
                Dirty = true;
            }
        }

        public bool ForceRescan
        {
            get { return forceRescan; }
            set
            {
                SetProperty(ref forceRescan, value);
                Dirty = true;
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
        }

        public bool Update()
        {
            if (!Version.Equals("3"))
            {
                BlogType = BlogTypes.tumblr;
                Version = "3";
                Dirty = true;
            }
            return true;
        }
    }
}
