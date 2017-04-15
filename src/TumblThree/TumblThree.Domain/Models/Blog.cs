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
        private bool downloadUrlList;
        private bool downloadVideo;
        private string lastDownloadedPhoto;
        private string lastDownloadedVideo;
        private string notes;
        private string tags;
        private int rating;

        public Blog()
        {
        }

        protected Blog(string url, string location, BlogTypes blogType)
        {
            Url = url;
            Url = ExtractUrl();
            Name = ExtractSubDomain();
            BlogType = blogType;
            ChildId = Path.Combine(location, Name + "_files." + blogType);
            Location = location;

            DateAdded = DateTime.Now;
            LastCompleteCrawl = new DateTime(0L, DateTimeKind.Utc);
        }

        [DataMember]
        public int DuplicatePhotos { get; set; }

        [DataMember]
        public int DuplicateVideos { get; set; }

        [DataMember]
        public int DuplicateAudios { get; set; }

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
        public PostTypes States { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public string Location { get; set; }

        [DataMember]
        public string ChildId { get; set; }

        public BlogTypes BlogType { get; set; }

        [DataMember]
        public int DownloadedImages { get; set; }

        [DataMember]
        public int TotalCount { get; set; }

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
        public int Posts { get; set; }

        [DataMember]
        public int Texts { get; set; }

        [DataMember]
        public int Quotes { get; set; }

        [DataMember]
        public int Photos { get; set; }

        [DataMember]
        public int NumberOfLinks { get; set; }

        [DataMember]
        public int Conversations { get; set; }

        [DataMember]
        public int Videos { get; set; }

        [DataMember]
        public int Audios { get; set; }

        [DataMember]
        public int PhotoMetas { get; set; }

        [DataMember]
        public int VideoMetas { get; set; }

        [DataMember]
        public int AudioMetas { get; set; }

        [DataMember]
        public int DownloadedTexts { get; set; }

        [DataMember]
        public int DownloadedQuotes { get; set; }

        [DataMember]
        public int DownloadedPhotos { get; set; }

        [DataMember]
        public int DownloadedLinks { get; set; }

        [DataMember]
        public int DownloadedConversations { get; set; }

        [DataMember]
        public int DownloadedVideos { get; set; }

        [DataMember]
        public int DownloadedAudios { get; set; }

        [DataMember]
        public int DownloadedPhotoMetas { get; set; }

        [DataMember]
        public int DownloadedVideoMetas { get; set; }

        [DataMember]
        public int DownloadedAudioMetas { get; set; }

        [DataMember]
        public DateTime DateAdded { get; set; }

        [DataMember]
        public DateTime LastCompleteCrawl { get; set; }

        [DataMember]
        public bool Online { get; set; }

        [DataMember]
        public int Progress { get; set; }

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
        public IList<string> Links { get; set; }

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
                    var serializer = new DataContractJsonSerializer(typeof(TumblrBlog));
                    var blog = (Blog)serializer.ReadObject(stream);
                    blog.Location = Path.Combine((Directory.GetParent(fileLocation).FullName));
                    blog.ChildId = Path.Combine(blog.Location, blog.Name + "_files.tumblr");
                    return blog;
                }
            }
            catch (ArgumentException ex)
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
                        var serializer = new DataContractJsonSerializer(typeof(TumblrBlog));
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
                        var serializer = new DataContractJsonSerializer(typeof(TumblrBlog));
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

        protected virtual string ExtractUrl()
        {
            return ("https://" + ExtractSubDomain() + ".tumblr.com/");
        }
    }
}
