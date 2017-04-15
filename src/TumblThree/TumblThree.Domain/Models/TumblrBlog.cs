using System.IO;
using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class TumblrBlog : Blog
    {
        private bool forceRescan;
        private bool forceSize;
        private bool skipGif;

        /// <summary>
        ///     DON'T use. Only for Mockup
        /// </summary>
        public TumblrBlog()
        {
        }

        public TumblrBlog(string url, string location, BlogTypes type) : base(url, location, type)
        {
            Version = "3";

            Directory.CreateDirectory(Location);
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Location).FullName, Name));

            if (!File.Exists(ChildId))
            {
                Files files = new TumblrFiles(Name, Location, BlogType);

                files.Save();
                files = null;
            }
        }

        [DataMember]
        public string Version { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public ulong LastId { get; set; }

        [DataMember]
        public bool SkipGif
        {
            get { return skipGif; }
            set
            {
                SetProperty(ref skipGif, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool ForceSize
        {
            get { return forceSize; }
            set
            {
                SetProperty(ref forceSize, value);
                Dirty = true;
            }
        }

        [DataMember]
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
