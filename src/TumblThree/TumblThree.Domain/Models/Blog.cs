using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Waf.Foundation;
using System.Xml;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class Blog : Model, IBlog
    {
        private bool checkDirectoryForFiles;
        private bool createAudioMeta;
        private bool createPhotoMeta;
        private bool createVideoMeta;
        private bool downloadAudio;
        private bool downloadConversation;
        private bool downloadLink;
        private bool downloadPhoto;
        private bool downloadQuote;
        private bool downloadText;
        private bool downloadAnswer;
        private bool downloadUrlList;
        private bool downloadVideo;
        private bool forceRescan = true;
        private bool forceSize;
        private string lastDownloadedPhoto;
        private string lastDownloadedVideo;
        private string notes;
        private int rating;
        private bool skipGif;
        private string tags;
        private int duplicatePhotos;
        private int duplicateVideos;
        private int duplicateAudios;
        private int totalCount;
        private int posts;
        private int texts;
        private int answers;
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
        private int downloadedAnswers;
        private int downloadedConversations;
        private int downloadedVideos;
        private int downloadedAudios;
        private int downloadedPhotoMetas;
        private int downloadedVideoMetas;
        private int downloadedAudioMetas;
        private string downloadPages;
        private int pageSize;
        private string downloadFrom;
        private string downloadTo;
        private bool downloadRebloggedPosts;
        private DateTime dateAdded;
        private DateTime lastCompleteCrawl;
        private bool online;
        private int progress;
        private int quotes;
        private List<string> links;
        private int downloadedImages;

        public Blog()
        {
        }

        public Blog(string url, string location, BlogTypes blogType)
        {
            Url = url;
            Url = ExtractUrl();
            Name = ExtractName();
            BlogType = blogType;
            ChildId = Path.Combine(location, Name + "_files." + blogType);
            Location = location;
            Version = "3";
            DateAdded = DateTime.Now;

            Directory.CreateDirectory(Location);
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Location).FullName, Name));

            if (!File.Exists(ChildId))
            {
                IFiles files = new Files(Name, Location, BlogType);

                files.Save();
                files = null;
            }
        }

        [DataMember]
        public PostTypes States { get; set; }

        [DataMember]
        public string Version { get; set; }

        [DataMember]
        public int DuplicatePhotos
        {
            get { return duplicatePhotos; }
            set { SetProperty(ref duplicatePhotos, value); }
        }

        [DataMember]
        public int DuplicateVideos
        {
            get { return duplicateVideos; }
            set { SetProperty(ref duplicateVideos, value); }
        }

        [DataMember]
        public int DuplicateAudios
        {
            get { return duplicateAudios; }
            set { SetProperty(ref duplicateAudios, value); }
        }

        [DataMember]
        public bool DownloadText
        {
            get { return downloadText; }
            set
            {
                SetProperty(ref downloadText, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadQuote
        {
            get { return downloadQuote; }
            set
            {
                SetProperty(ref downloadQuote, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadPhoto
        {
            get { return downloadPhoto; }
            set
            {
                SetProperty(ref downloadPhoto, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadLink
        {
            get { return downloadLink; }
            set
            {
                SetProperty(ref downloadLink, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadAnswer
        {
            get { return downloadAnswer; }
            set
            {
                SetProperty(ref downloadAnswer, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadConversation
        {
            get { return downloadConversation; }
            set
            {
                SetProperty(ref downloadConversation, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadVideo
        {
            get { return downloadVideo; }
            set
            {
                SetProperty(ref downloadVideo, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadAudio
        {
            get { return downloadAudio; }
            set
            {
                SetProperty(ref downloadAudio, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool CreatePhotoMeta
        {
            get { return createPhotoMeta; }
            set
            {
                SetProperty(ref createPhotoMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool CreateVideoMeta
        {
            get { return createVideoMeta; }
            set
            {
                SetProperty(ref createVideoMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool CreateAudioMeta
        {
            get { return createAudioMeta; }
            set
            {
                SetProperty(ref createAudioMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadRebloggedPosts
        {
            get { return downloadRebloggedPosts; }
            set
            {
                SetProperty(ref downloadRebloggedPosts, value);
                Dirty = true;
            }
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public string Location { get; set; }

        [DataMember]
        public string ChildId { get; set; }

        [DataMember]
        public BlogTypes BlogType { get; set; }

        [DataMember]
        public int DownloadedImages
        {
            get { return downloadedImages; }
            set { SetProperty(ref downloadedImages, value); }
        }
        [DataMember]
        public int TotalCount
        {
            get { return totalCount; }
            set { SetProperty(ref totalCount, value); }
        }

        [DataMember]
        public string Tags
        {
            get { return tags; }
            set
            {
                SetProperty(ref tags, value);
                Dirty = true;
            }
        }

        [DataMember]
        public int Rating
        {
            get { return rating; }
            set
            {
                SetProperty(ref rating, value);
                Dirty = true;
            }
        }

        [DataMember]
        public int Posts
        {
            get { return posts; }
            set { SetProperty(ref posts, value); }
        }

        [DataMember]
        public int Texts
        {
            get { return texts; }
            set { SetProperty(ref texts, value); }
        }

        [DataMember]
        public int Answers
        {
            get { return answers; }
            set { SetProperty(ref answers, value); }
        }

        [DataMember]
        public int Quotes
        {
            get { return quotes; }
            set { SetProperty(ref quotes, value); }
        }

        [DataMember]
        public int Photos
        {
            get { return photos; }
            set { SetProperty(ref photos, value); }
        }

        [DataMember]
        public int NumberOfLinks
        {
            get { return numberOfLinks; }
            set { SetProperty(ref numberOfLinks, value); }
        }

        [DataMember]
        public int Conversations
        {
            get { return conversations; }
            set { SetProperty(ref conversations, value); }
        }

        [DataMember]
        public int Videos
        {
            get { return videos; }
            set { SetProperty(ref videos, value); }
        }

        [DataMember]
        public int Audios
        {
            get { return audios; }
            set { SetProperty(ref audios, value); }
        }

        [DataMember]
        public int PhotoMetas
        {
            get { return photoMetas; }
            set { SetProperty(ref photoMetas, value); }
        }

        [DataMember]
        public int VideoMetas
        {
            get { return videoMetas; }
            set { SetProperty(ref videoMetas, value); }
        }

        [DataMember]
        public int AudioMetas
        {
            get { return audioMetas; }
            set { SetProperty(ref audioMetas, value); }
        }

        [DataMember]
        public int DownloadedTexts
        {
            get { return downloadedTexts; }
            set { SetProperty(ref downloadedTexts, value); }
        }

        [DataMember]
        public int DownloadedQuotes
        {
            get { return downloadedQuotes; }
            set { SetProperty(ref downloadedQuotes, value); }
        }

        [DataMember]
        public int DownloadedPhotos
        {
            get { return downloadedPhotos; }
            set { SetProperty(ref downloadedPhotos, value); }
        }

        [DataMember]
        public int DownloadedLinks
        {
            get { return downloadedLinks; }
            set { SetProperty(ref downloadedLinks, value); }
        }

        [DataMember]
        public int DownloadedConversations
        {
            get { return downloadedConversations; }
            set { SetProperty(ref downloadedConversations, value); }
        }

        [DataMember]
        public int DownloadedAnswers
        {
            get { return downloadedAnswers; }
            set { SetProperty(ref downloadedAnswers, value); }
        }

        [DataMember]
        public int DownloadedVideos
        {
            get { return downloadedVideos; }
            set { SetProperty(ref downloadedVideos, value); }
        }

        [DataMember]
        public int DownloadedAudios
        {
            get { return downloadedAudios; }
            set { SetProperty(ref downloadedAudios, value); }
        }

        [DataMember]
        public int DownloadedPhotoMetas
        {
            get { return downloadedPhotoMetas; }
            set { SetProperty(ref downloadedPhotoMetas, value); }
        }

        [DataMember]
        public int DownloadedVideoMetas
        {
            get { return downloadedVideoMetas; }
            set { SetProperty(ref downloadedVideoMetas, value); }
        }

        [DataMember]
        public int DownloadedAudioMetas
        {
            get { return downloadedAudioMetas; }
            set { SetProperty(ref downloadedAudioMetas, value); }
        }

        [DataMember]
        public string DownloadPages
        {
            get { return downloadPages; }
            set
            {
                SetProperty(ref downloadPages, value);
                Dirty = true;
            }
        }

        [DataMember]
        public int PageSize
        {
            get { return pageSize; }
            set
            {
                SetProperty(ref pageSize, value);
                Dirty = true;
            }
        }

        [DataMember]
        public string DownloadFrom
        {
            get { return downloadFrom; }
            set
            {
                SetProperty(ref downloadFrom, value);
                Dirty = true;
            }
        }

        [DataMember]
        public string DownloadTo
        {
            get { return downloadTo; }
            set
            {
                SetProperty(ref downloadTo, value);
                Dirty = true;
            }
        }

        [DataMember]
        public DateTime DateAdded
        {
            get { return dateAdded; }
            set { SetProperty(ref dateAdded, value); }
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime LastCompleteCrawl
        {
            get { return lastCompleteCrawl; }
            set { SetProperty(ref lastCompleteCrawl, value); }
        }

        [DataMember]
        public bool Online
        {
            get { return online; }
            set { SetProperty(ref online, value); }
        }

        [DataMember]
        public int Progress
        {
            get { return progress; }
            set { SetProperty(ref progress, value); }
        }

        [DataMember]
        public string Notes
        {
            get { return notes; }
            set
            {
                SetProperty(ref notes, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool CheckDirectoryForFiles
        {
            get { return checkDirectoryForFiles; }
            set
            {
                SetProperty(ref checkDirectoryForFiles, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadUrlList
        {
            get { return downloadUrlList; }
            set
            {
                SetProperty(ref downloadUrlList, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool Dirty { get; set; }

        [DataMember]
        public Exception LoadError { get; set; }

        [DataMember]
        public List<string> Links
        {
            get { return links; }
            set { SetProperty(ref links, value); }
        }

        [DataMember]
        public string LastDownloadedPhoto
        {
            get { return lastDownloadedPhoto; }
            set
            {
                SetProperty(ref lastDownloadedPhoto, value);
                States = PostTypes.Photo;
            }
        }

        [DataMember]
        public string LastDownloadedVideo
        {
            get { return lastDownloadedVideo; }
            set
            {
                SetProperty(ref lastDownloadedVideo, value);
                States = PostTypes.Video;
            }
        }

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
                    var serializer = new DataContractJsonSerializer(GetType());
                    var blog = (Blog)serializer.ReadObject(stream);
                    blog.Location = Path.Combine((Directory.GetParent(fileLocation).FullName));
                    blog.ChildId = Path.Combine(blog.Location, blog.Name + "_files." + blog.BlogType);
                    return blog;
                }
            }
            catch (SerializationException ex)
            {
                ex.Data.Add("Filename", fileLocation);
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
            string currentIndex = Path.Combine(Location, Name + "." + BlogType);
            string newIndex = Path.Combine(Location, Name + "." + BlogType + ".new");
            string backupIndex = Path.Combine(Location, Name + "." + BlogType + ".bak");

            if (File.Exists(currentIndex))
            {
                using (var stream = new FileStream(newIndex, FileMode.Create, FileAccess.Write))
                {
                    using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(
                        stream, Encoding.UTF8, true, true, "  "))
                    {
                        var serializer = new DataContractJsonSerializer(GetType());
                        serializer.WriteObject(writer, this);
                        writer.Flush();
                    }
                }
                File.Replace(newIndex, currentIndex, backupIndex, true);
                File.Delete(backupIndex);
            }
            else
            {
                using (var stream = new FileStream(currentIndex, FileMode.Create, FileAccess.Write))
                {
                    using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(
                        stream, Encoding.UTF8, true, true, "  "))
                    {
                        var serializer = new DataContractJsonSerializer(GetType());
                        serializer.WriteObject(writer, this);
                        writer.Flush();
                    }
                }
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

        protected virtual string ExtractName()
        {
            return ExtractSubDomain();
        }

        protected virtual string ExtractUrl()
        {
            return ("https://" + ExtractSubDomain() + ".tumblr.com/");
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
        }
    }
}
