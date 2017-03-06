using System;
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
        private uint photoMetas;
        private uint videoMetas;
        private uint audioMetas;
        private uint downloadedTexts;
        private uint downloadedQuotes;
        private uint downloadedPhotos;
        private uint downloadedLinks;
        private uint downloadedConversations;
        private uint downloadedVideos;
        private uint downloadedAudios;
        private uint downloadedPhotoMetas;
        private uint downloadedVideoMetas;
        private uint downloadedAudioMetas;
        private uint duplicatePhotos;
        private uint duplicateVideos;
        private uint duplicateAudios;
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
        private postTypes state;

        public enum postTypes
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

        public TumblrBlog(string url)
        {
            this.version = "3";
            this.childId = String.Empty;
            this.description = String.Empty;
            this.Url = url;
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

        public uint PhotoMetas
        {
            get { return photoMetas; }
            set { SetProperty(ref photoMetas, value); }
        }

        public uint VideoMetas
        {
            get { return videoMetas; }
            set { SetProperty(ref videoMetas, value); }
        }

        public uint AudioMetas
        {
            get { return audioMetas; }
            set { SetProperty(ref audioMetas, value); }
        }

        public uint DownloadedTexts
        {
            get { return downloadedTexts; }
            set { SetProperty(ref downloadedTexts, value); }
        }

        public uint DownloadedQuotes
        {
            get { return downloadedQuotes; }
            set { SetProperty(ref downloadedQuotes, value); }
        }

        public uint DownloadedPhotos
        {
            get { return downloadedPhotos; }
            set { SetProperty(ref downloadedPhotos, value); }
        }

        public uint DownloadedLinks
        {
            get { return downloadedLinks; }
            set { SetProperty(ref downloadedLinks, value); }
        }

        public uint DownloadedConversations
        {
            get { return downloadedConversations; }
            set { SetProperty(ref downloadedConversations, value); }
        }

        public uint DownloadedVideos
        {
            get { return downloadedVideos; }
            set { SetProperty(ref downloadedVideos, value); }
        }

        public uint DownloadedAudios
        {
            get { return downloadedAudios; }
            set { SetProperty(ref downloadedAudios, value); }
        }

        public uint DownloadedPhotoMetas
        {
            get { return downloadedPhotoMetas; }
            set { SetProperty(ref downloadedPhotoMetas, value); }
        }

        public uint DownloadedVideoMetas
        {
            get { return downloadedVideoMetas; }
            set { SetProperty(ref downloadedVideoMetas, value); }
        }

        public uint DownloadedAudioMetas
        {
            get { return downloadedAudioMetas; }
            set { SetProperty(ref downloadedAudioMetas, value); }
        }

        public uint DuplicatePhotos
        {
            get { return duplicatePhotos; }
            set { SetProperty(ref duplicatePhotos, value); }
        }

        public uint DuplicateVideos
        {
            get { return duplicateVideos; }
            set { SetProperty(ref duplicateVideos, value); }
        }

        public uint DuplicateAudios
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
            set { SetProperty(ref lastDownloadedPhoto, value); States = postTypes.Photo; }
        }

        public string LastDownloadedVideo
        {
            get { return lastDownloadedVideo; }
            set { SetProperty(ref lastDownloadedVideo, value); States = postTypes.Video; }
        }

        public postTypes States
        {
            get { return state; }
            set { SetProperty(ref state, value); }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
        }
    }
}
