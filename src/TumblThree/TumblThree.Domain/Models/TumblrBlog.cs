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
        private string childId;
        private string description;
        private string title;
        private ulong lastId;
        private int progress;
        private string tags;
        private int posts;
        private int texts;
        private int quotes;
        private int photos;
        private int numberOfLinks;
        private int conversations;
        private int videos;
        private int audios;
        private int photoMetas;
        private int videoMetas;
        private int audioMetas;
        private int downloadedTexts;
        private int downloadedQuotes;
        private int downloadedPhotos;
        private int downloadedLinks;
        private int downloadedConversations;
        private int downloadedVideos;
        private int downloadedAudios;
        private int downloadedPhotoMetas;
        private int downloadedVideoMetas;
        private int downloadedAudioMetas;
        private int duplicatePhotos;
        private int duplicateVideos;
        private int duplicateAudios;
        private string lastDownloadedPhoto;
        private string lastDownloadedVideo;
        private bool downloadPhoto;
        private bool downloadVideo;
        private bool downloadAudio;
        private bool downloadText;
        private bool downloadQuote;
        private bool downloadConversation;
        private bool downloadLink;
        private bool createPhotoMeta;
        private bool createVideoMeta;
        private bool createAudioMeta;
        private bool skipGif;
        private bool forceSize;
        private bool forceRescan;
        private PostTypes state;

        public enum PostTypes
        {
            Photo,
            Video,
            Audio,
            Text,
            Quote,
            Conversation,
            Link
        }

        public TumblrBlog()
        {
            this.version = "3";
            this.childId = String.Empty;
            this.description = String.Empty;
            this.title = String.Empty;
            this.lastId = 0;
            this.progress = 0;
            this.tags = String.Empty;
            this.posts = 0;
            this.texts = 0;
            this.quotes = 0;
            this.photos = 0;
            this.numberOfLinks = 0;
            this.conversations = 0;
            this.videos = 0;
            this.audios = 0;
            this.photoMetas = 0;
            this.videoMetas = 0;
            this.audioMetas = 0;
            this.downloadedTexts = 0;
            this.downloadedQuotes = 0;
            this.downloadedPhotos = 0;
            this.downloadedLinks = 0;
            this.downloadedConversations = 0;
            this.downloadedVideos = 0;
            this.downloadedAudios = 0;
            this.downloadedPhotoMetas = 0;
            this.downloadedVideoMetas = 0;
            this.downloadedAudioMetas = 0;
            this.duplicatePhotos = 0;
            this.duplicateAudios = 0;
            this.duplicateVideos = 0;
            this.downloadText = false;
            this.downloadQuote = false;
            this.downloadPhoto = true;
            this.downloadLink = false;
            this.downloadConversation = false;
            this.downloadVideo = false;
            this.downloadAudio = false;
            this.createPhotoMeta = false;
            this.createVideoMeta = false;
            this.createAudioMeta = false;
            this.skipGif = false;
            this.forceSize = false;
            this.forceRescan = false;
            this.lastDownloadedPhoto = null;
            this.lastDownloadedVideo = null;
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

        public string ChildId
        {
            get { return childId; }
            set { SetProperty(ref childId, value); }
        }

        public ulong LastId
        {
            get { return lastId; }
            set { SetProperty(ref lastId, value); }
        }

        public int Progress
        {
            get { return progress; }
            set { SetProperty(ref progress, value); }
        }

        public string Tags
        {
            get { return tags; }
            set { SetProperty(ref tags, value); Dirty = true; }
        }

        public int Posts
        {
            get { return posts; }
            set { SetProperty(ref posts, value); }
        }

        public int Texts
        {
            get { return texts; }
            set { SetProperty(ref texts, value); }
        }

        public int Quotes
        {
            get { return quotes; }
            set { SetProperty(ref quotes, value); }
        }

        public int Photos
        {
            get { return photos; }
            set { SetProperty(ref photos, value); }
        }

        public int NumberOfLinks
        {
            get { return numberOfLinks; }
            set { SetProperty(ref numberOfLinks, value); }
        }

        public int Conversations
        {
            get { return conversations; }
            set { SetProperty(ref conversations, value); }
        }

        public int Videos
        {
            get { return videos; }
            set { SetProperty(ref videos, value); }
        }

        public int Audios
        {
            get { return audios; }
            set { SetProperty(ref audios, value); }
        }

        public int PhotoMetas
        {
            get { return photoMetas; }
            set { SetProperty(ref photoMetas, value); }
        }

        public int VideoMetas
        {
            get { return videoMetas; }
            set { SetProperty(ref videoMetas, value); }
        }

        public int AudioMetas
        {
            get { return audioMetas; }
            set { SetProperty(ref audioMetas, value); }
        }

        public int DownloadedTexts
        {
            get { return downloadedTexts; }
            set { SetProperty(ref downloadedTexts, value); }
        }

        public int DownloadedQuotes
        {
            get { return downloadedQuotes; }
            set { SetProperty(ref downloadedQuotes, value); }
        }

        public int DownloadedPhotos
        {
            get { return downloadedPhotos; }
            set { SetProperty(ref downloadedPhotos, value); }
        }

        public int DownloadedLinks
        {
            get { return downloadedLinks; }
            set { SetProperty(ref downloadedLinks, value); }
        }

        public int DownloadedConversations
        {
            get { return downloadedConversations; }
            set { SetProperty(ref downloadedConversations, value); }
        }

        public int DownloadedVideos
        {
            get { return downloadedVideos; }
            set { SetProperty(ref downloadedVideos, value); }
        }

        public int DownloadedAudios
        {
            get { return downloadedAudios; }
            set { SetProperty(ref downloadedAudios, value); }
        }

        public int DownloadedPhotoMetas
        {
            get { return downloadedPhotoMetas; }
            set { SetProperty(ref downloadedPhotoMetas, value); }
        }

        public int DownloadedVideoMetas
        {
            get { return downloadedVideoMetas; }
            set { SetProperty(ref downloadedVideoMetas, value); }
        }

        public int DownloadedAudioMetas
        {
            get { return downloadedAudioMetas; }
            set { SetProperty(ref downloadedAudioMetas, value); }
        }

        public int DuplicatePhotos
        {
            get { return duplicatePhotos; }
            set { SetProperty(ref duplicatePhotos, value); }
        }

        public int DuplicateVideos
        {
            get { return duplicateVideos; }
            set { SetProperty(ref duplicateVideos, value); }
        }

        public int DuplicateAudios
        {
            get { return duplicateAudios; }
            set { SetProperty(ref duplicateAudios, value); }
        }

        public bool DownloadText
        {
            get { return downloadText; }
            set { SetProperty(ref downloadText, value); Dirty = true; }
        }

        public bool DownloadQuote
        {
            get { return downloadQuote; }
            set { SetProperty(ref downloadQuote, value); Dirty = true; }
        }

        public bool DownloadPhoto
        {
            get { return downloadPhoto; }
            set { SetProperty(ref downloadPhoto, value); Dirty = true; }
        }

        public bool DownloadLink
        {
            get { return downloadLink; }
            set { SetProperty(ref downloadLink, value); Dirty = true; }
        }

        public bool DownloadConversation
        {
            get { return downloadConversation; }
            set { SetProperty(ref downloadConversation, value); Dirty = true; }
        }

        public bool DownloadVideo
        {
            get { return downloadVideo; }
            set { SetProperty(ref downloadVideo, value); Dirty = true; }
        }

        public bool DownloadAudio
        {
            get { return downloadAudio; }
            set { SetProperty(ref downloadAudio, value); Dirty = true; }
        }

        public bool CreatePhotoMeta
        {
            get { return createPhotoMeta; }
            set { SetProperty(ref createPhotoMeta, value); Dirty = true; }
        }

        public bool CreateVideoMeta
        {
            get { return createVideoMeta; }
            set { SetProperty(ref createVideoMeta, value); Dirty = true; }
        }

        public bool CreateAudioMeta
        {
            get { return createAudioMeta; }
            set { SetProperty(ref createAudioMeta, value); Dirty = true; }
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

        public string LastDownloadedPhoto
        {
            get { return lastDownloadedPhoto; }
            set { SetProperty(ref lastDownloadedPhoto, value); States = PostTypes.Photo; }
        }

        public string LastDownloadedVideo
        {
            get { return lastDownloadedVideo; }
            set { SetProperty(ref lastDownloadedVideo, value); States = PostTypes.Video; }
        }

        public PostTypes States
        {
            get { return state; }
            set { SetProperty(ref state, value); }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
        }

        public bool Update()
        {
            if (string.IsNullOrEmpty(this.Version) || !this.Version.Equals("3"))
            {
                if (!File.Exists(this.ChildId))
                {
                    TumblrFiles files = new TumblrFiles();
                    files.Location = this.Location;
                    files.Name = this.Name;
                    files.Links = this.Links.Select(item => item?.Split('/').Last()).ToList();
                    this.Links.Clear();
                    this.Version = "3";
                    this.Type = BlogTypes.tumblr;
                    this.Dirty = true;
                    files.Save();
                    files = null;
                }
            } else if (this.Version.Equals("2"))
            {
                this.Type = BlogTypes.tumblr;
                this.Version = "3";
                this.Dirty = true;
            }

            return true;
        }
    }
}
