using System;

namespace TumblThree.Domain.Models
{
    [Serializable]
    public class TumblrBlog : Blog
    {
        private string description;
        private string title;
        private uint progress;
        private string tags;
        private uint posts;
        private uint texts;
        private uint quotes;
        private uint photos;
        private uint numberOfLinks;
        private uint conversations;
        private uint videos;
        private uint audios;

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
            this.posts = 0;
            this.texts = 0;
            this.quotes = 0;
            this.photos = 0;
            this.numberOfLinks = 0;
            this.conversations = 0;
            this.videos = 0;
            this.audios = 0;
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
            set { SetProperty(ref tags, value); Dirty = true; }
        }

        public uint Posts
        {
            get { return posts; }
            set { SetProperty(ref posts, value); }
        }

        public uint Texts
        {
            get { return texts; }
            set { SetProperty(ref texts, value); }
        }

        public uint Quotes
        {
            get { return quotes; }
            set { SetProperty(ref quotes, value); }
        }

        public uint Photos
        {
            get { return photos; }
            set { SetProperty(ref photos, value); }
        }

        public uint NumberOfLinks
        {
            get { return numberOfLinks; }
            set { SetProperty(ref numberOfLinks, value); }
        }

        public uint Conversations
        {
            get { return conversations; }
            set { SetProperty(ref conversations, value); }
        }

        public uint Videos
        {
            get { return videos; }
            set { SetProperty(ref videos, value); }
        }

        public uint Audios
        {
            get { return audios; }
            set { SetProperty(ref audios, value); }
        }
    }
}
