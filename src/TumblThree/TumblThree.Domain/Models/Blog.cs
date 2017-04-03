using System;
using System.Waf.Foundation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace TumblThree.Domain.Models
{
    [Serializable]
    public abstract class Blog : Model, IBlog
    {

        private string name;
        private string url;
        private string location;
        private string childId;
        private BlogTypes blogType;
        private int downloadedImages;
        private int totalCount;
        private int rating;
        private int progress;
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
        private DateTime dateAdded;
        private DateTime lastCompleteCrawl;
        private bool online;
        private string lastDownloadedPhoto;
        private string lastDownloadedVideo;
        private bool dirty;
        private bool checkDirectoryForFiles;
        private bool downloadUrlList;
        private string notes;
        private IList<string> links;
        private Exception loadError;
        private PostTypes state;


        protected Blog()
        {
        }

        protected Blog(string url, string location, BlogTypes blogType)
        {
            this.url = url;
            this.url = ExtractUrl();
            this.name = ExtractSubDomain();
            this.blogType = blogType;
            this.childId = Path.Combine(location, Name + "_files." + blogType);
            this.location = location;
            this.downloadedImages = 0;
            this.totalCount = 0;
            this.rating = 0;
            this.progress = 0;
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
            this.dateAdded = DateTime.Now;
            this.lastCompleteCrawl = DateTime.MinValue;
            this.online = false;
            this.lastDownloadedPhoto = null;
            this.lastDownloadedVideo = null;
            this.checkDirectoryForFiles = false;
            this.dirty = false;
            this.notes = String.Empty;
            this.links = new ObservableCollection<string>();
        }

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public string Url
        {
            get { return url; }
            set { SetProperty(ref url, value); }
        }

        public string Location
        {
            get { return location; }
            set { SetProperty(ref location, value); }
        }

        public string ChildId
        {
            get { return childId; }
            set { SetProperty(ref childId, value); }
        }

        public BlogTypes BlogType
        {
            get { return blogType; }
            set { SetProperty(ref blogType, value); }
        }

        public int DownloadedImages
        {
            get { return downloadedImages; }
            set { SetProperty(ref downloadedImages, value); }
        }

        public int TotalCount
        {
            get { return totalCount; }
            set { SetProperty(ref totalCount, value); }
        }

        public int Rating
        {
            get { return rating; }
            set { SetProperty(ref rating, value); Dirty = true; }
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

        public DateTime DateAdded
        {
            get { return dateAdded; }
            set { SetProperty(ref dateAdded, value); }
        }

        public DateTime LastCompleteCrawl
        {
            get { return lastCompleteCrawl; }
            set { SetProperty(ref lastCompleteCrawl, value); }
        }

        public bool Online
        {
            get { return online; }
            set { SetProperty(ref online, value); }
        }

        public int Progress
        {
            get { return progress; }
            set { SetProperty(ref progress, value); }
        }

        public string Notes
        {
            get { return notes; }
            set { SetProperty(ref notes, value); Dirty = true; }
        }

        public bool CheckDirectoryForFiles
        {
            get { return checkDirectoryForFiles; }
            set { SetProperty(ref checkDirectoryForFiles, value); Dirty = true; }
        }

        public bool DownloadUrlList
        {
            get { return downloadUrlList; }
            set { SetProperty(ref downloadUrlList, value); Dirty = true; }
        }

        public bool Dirty
        {
            get { return dirty; }
            set { SetProperty(ref dirty, value); }
        }

        public Exception LoadError
        {
            get { return loadError; }
            set { SetProperty(ref loadError, value); }
        }

        public IList<string> Links
        {
            get { return links; }
            set { SetProperty(ref links, value); }
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

        private void SaveBlog()
        {
            string currentIndex = Path.Combine(location, this.Name + "." + this.BlogType);
            string newIndex = Path.Combine(location, this.Name + "." + this.BlogType + ".new");
            string backupIndex = Path.Combine(location, this.Name + "." + this.BlogType + ".bak");

            if (File.Exists(currentIndex))
            {
                System.Web.Script.Serialization.JavaScriptSerializer jsJson =
                    new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = 2147483644 };
                File.WriteAllText(newIndex, JsonFormatter.FormatOutput(jsJson.Serialize(this)));
                File.Replace(newIndex, currentIndex, backupIndex, true);
                File.Delete(backupIndex);
            }
            else
            {
                System.Web.Script.Serialization.JavaScriptSerializer jsJson =
                    new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = 2147483644 };
                File.WriteAllText(currentIndex, JsonFormatter.FormatOutput(jsJson.Serialize(this)));
            }
        }

        public bool Save()
        {
            try
            {
                this.Dirty = false;
                SaveBlog();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Blog:Save: {0}", ex);
                throw;
            }
        }

        protected virtual string ExtractSubDomain()
        {
            string[] source = this.Url.Split(new char[] { '.' });
            if ((source.Count<string>() >= 3) && source[0].StartsWith("http://", true, null))
            {
                return source[0].Replace("http://", string.Empty);
            }
            else if ((source.Count<string>() >= 3) && source[0].StartsWith("https://", true, null))
            {
                return source[0].Replace("https://", string.Empty);
            }
            return null;
        }

        protected virtual string ExtractUrl()
        {
            return ("https://" + ExtractSubDomain() + ".tumblr.com/");
        }
    }
}
