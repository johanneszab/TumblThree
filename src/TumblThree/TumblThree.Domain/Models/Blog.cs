using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Waf.Foundation;

namespace TumblThree.Domain.Models
{
    [Serializable]
    public class Blog : Model, IBlog
    {
        private int audioMetas;
        private int audios;
        private BlogTypes blogType;
        private bool checkDirectoryForFiles;
        private string childId;
        private int conversations;
        private bool createAudioMeta;
        private bool createPhotoMeta;
        private bool createVideoMeta;
        private DateTime dateAdded;
        private bool dirty;
        private bool downloadAudio;
        private bool downloadConversation;
        private int downloadedAudioMetas;
        private int downloadedAudios;
        private int downloadedConversations;
        private int downloadedImages;
        private int downloadedLinks;
        private int downloadedPhotoMetas;
        private int downloadedPhotos;
        private int downloadedQuotes;
        private int downloadedTexts;
        private int downloadedVideoMetas;
        private int downloadedVideos;
        private bool downloadLink;
        private bool downloadPhoto;
        private bool downloadQuote;
        private bool downloadText;
        private bool downloadUrlList;
        private bool downloadVideo;
        private int duplicateAudios;
        private int duplicatePhotos;
        private int duplicateVideos;
        private DateTime lastCompleteCrawl;
        private string lastDownloadedPhoto;
        private string lastDownloadedVideo;
        private IList<string> links;
        private Exception loadError;
        private string location;

        private string name;
        private string notes;
        private int numberOfLinks;
        private bool online;
        private int photoMetas;
        private int photos;
        private int posts;
        private int progress;
        private int quotes;
        private int rating;
        private PostTypes state;
        private int texts;
        private int totalCount;
        private string url;
        private int videoMetas;
        private int videos;

        public Blog()
        {
        }

        protected Blog(string url, string location, BlogTypes blogType)
        {
            this.url = url;
            this.url = ExtractUrl();
            name = ExtractSubDomain();
            this.blogType = blogType;
            childId = Path.Combine(location, Name + "_files." + blogType);
            this.location = location;
            downloadedImages = 0;
            totalCount = 0;
            rating = 0;
            progress = 0;
            posts = 0;
            texts = 0;
            quotes = 0;
            photos = 0;
            numberOfLinks = 0;
            conversations = 0;
            videos = 0;
            audios = 0;
            photoMetas = 0;
            videoMetas = 0;
            audioMetas = 0;
            downloadedTexts = 0;
            downloadedQuotes = 0;
            downloadedPhotos = 0;
            downloadedLinks = 0;
            downloadedConversations = 0;
            downloadedVideos = 0;
            downloadedAudios = 0;
            downloadedPhotoMetas = 0;
            downloadedVideoMetas = 0;
            downloadedAudioMetas = 0;
            duplicatePhotos = 0;
            duplicateAudios = 0;
            duplicateVideos = 0;
            downloadText = false;
            downloadQuote = false;
            downloadPhoto = true;
            downloadLink = false;
            downloadConversation = false;
            downloadVideo = false;
            downloadAudio = false;
            createPhotoMeta = false;
            createVideoMeta = false;
            createAudioMeta = false;
            dateAdded = DateTime.Now;
            lastCompleteCrawl = DateTime.MinValue;
            online = false;
            lastDownloadedPhoto = null;
            lastDownloadedVideo = null;
            checkDirectoryForFiles = false;
            dirty = false;
            notes = string.Empty;
            links = new ObservableCollection<string>();
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
            set
            {
                SetProperty(ref downloadText, value);
                Dirty = true;
            }
        }

        public bool DownloadQuote
        {
            get { return downloadQuote; }
            set
            {
                SetProperty(ref downloadQuote, value);
                Dirty = true;
            }
        }

        public bool DownloadPhoto
        {
            get { return downloadPhoto; }
            set
            {
                SetProperty(ref downloadPhoto, value);
                Dirty = true;
            }
        }

        public bool DownloadLink
        {
            get { return downloadLink; }
            set
            {
                SetProperty(ref downloadLink, value);
                Dirty = true;
            }
        }

        public bool DownloadConversation
        {
            get { return downloadConversation; }
            set
            {
                SetProperty(ref downloadConversation, value);
                Dirty = true;
            }
        }

        public bool DownloadVideo
        {
            get { return downloadVideo; }
            set
            {
                SetProperty(ref downloadVideo, value);
                Dirty = true;
            }
        }

        public bool DownloadAudio
        {
            get { return downloadAudio; }
            set
            {
                SetProperty(ref downloadAudio, value);
                Dirty = true;
            }
        }

        public bool CreatePhotoMeta
        {
            get { return createPhotoMeta; }
            set
            {
                SetProperty(ref createPhotoMeta, value);
                Dirty = true;
            }
        }

        public bool CreateVideoMeta
        {
            get { return createVideoMeta; }
            set
            {
                SetProperty(ref createVideoMeta, value);
                Dirty = true;
            }
        }

        public bool CreateAudioMeta
        {
            get { return createAudioMeta; }
            set
            {
                SetProperty(ref createAudioMeta, value);
                Dirty = true;
            }
        }

        public PostTypes States
        {
            get { return state; }
            set { SetProperty(ref state, value); }
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
            set
            {
                SetProperty(ref rating, value);
                Dirty = true;
            }
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
            set
            {
                SetProperty(ref notes, value);
                Dirty = true;
            }
        }

        public bool CheckDirectoryForFiles
        {
            get { return checkDirectoryForFiles; }
            set
            {
                SetProperty(ref checkDirectoryForFiles, value);
                Dirty = true;
            }
        }

        public bool DownloadUrlList
        {
            get { return downloadUrlList; }
            set
            {
                SetProperty(ref downloadUrlList, value);
                Dirty = true;
            }
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
            set
            {
                SetProperty(ref lastDownloadedPhoto, value);
                States = PostTypes.Photo;
            }
        }

        public string LastDownloadedVideo
        {
            get { return lastDownloadedVideo; }
            set
            {
                SetProperty(ref lastDownloadedVideo, value);
                States = PostTypes.Video;
            }
        }

        public string DownloadLocation()
        {
            return Path.Combine((Directory.GetParent(Location).FullName), Name);
        }

        public IBlog Load(string fileLocation)
        {
            try
            {
                using (var stream = new FileStream(fileLocation,
                    FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    string json = File.ReadAllText(fileLocation);
                    var blog = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<TumblrBlog>(json);
                    blog.Location = Path.Combine((Directory.GetParent(fileLocation).FullName));
                    blog.ChildId = Path.Combine(blog.Location, blog.Name + "_files.tumblr");
                    blog.Update();
                    return blog;
                }
            }
            catch (SerializationException ex)
            {
                ex.Data["Filename"] = fileLocation;
                throw;
            }
        }

        public bool Save()
        {
            try
            {
                Dirty = false;
                SaveBlog();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Blog:Save: {0}", ex);
                throw;
            }
        }

        private void SaveBlog()
        {
            string currentIndex = Path.Combine(location, Name + "." + BlogType);
            string newIndex = Path.Combine(location, Name + "." + BlogType + ".new");
            string backupIndex = Path.Combine(location, Name + "." + BlogType + ".bak");

            if (File.Exists(currentIndex))
            {
                var jsJson =
                    new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = 2147483644 };
                File.WriteAllText(newIndex, JsonFormatter.FormatOutput(jsJson.Serialize(this)));
                File.Replace(newIndex, currentIndex, backupIndex, true);
                File.Delete(backupIndex);
            }
            else
            {
                var jsJson =
                    new System.Web.Script.Serialization.JavaScriptSerializer { MaxJsonLength = 2147483644 };
                File.WriteAllText(currentIndex, JsonFormatter.FormatOutput(jsJson.Serialize(this)));
            }
        }

        protected virtual string ExtractSubDomain()
        {
            string[] source = Url.Split(new char[] { '.' });
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
