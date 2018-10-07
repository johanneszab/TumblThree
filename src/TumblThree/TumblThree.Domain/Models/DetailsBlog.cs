using System.Runtime.Serialization;

using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class DetailsBlog : Blog
    {
        private bool? checkDirectoryForFiles;
        private bool? createAudioMeta;
        private bool? createPhotoMeta;
        private bool? createVideoMeta;
        private bool? downloadAudio;
        private bool? downloadConversation;
        private bool? downloadLink;
        private bool? downloadPhoto;
        private bool? downloadQuote;
        private bool? downloadText;
        private bool? downloadAnswer;
        private bool? downloadUrlList;
        private bool? downloadVideo;
        private bool? forceRescan;
        private bool? forceSize;
        private bool? skipGif;
        private bool? downloadRebloggedPosts;
        private bool? online;

        [DataMember]
        public new bool? DownloadText
        {
            get => downloadText;
            set
            {
                SetProperty(ref downloadText, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadQuote
        {
            get => downloadQuote;
            set
            {
                SetProperty(ref downloadQuote, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadPhoto
        {
            get => downloadPhoto;
            set
            {
                SetProperty(ref downloadPhoto, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadLink
        {
            get => downloadLink;
            set
            {
                SetProperty(ref downloadLink, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadAnswer
        {
            get => downloadAnswer;
            set
            {
                SetProperty(ref downloadAnswer, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadConversation
        {
            get => downloadConversation;
            set
            {
                SetProperty(ref downloadConversation, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadVideo
        {
            get => downloadVideo;
            set
            {
                SetProperty(ref downloadVideo, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadAudio
        {
            get => downloadAudio;
            set
            {
                SetProperty(ref downloadAudio, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? CreatePhotoMeta
        {
            get => createPhotoMeta;
            set
            {
                SetProperty(ref createPhotoMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? CreateVideoMeta
        {
            get => createVideoMeta;
            set
            {
                SetProperty(ref createVideoMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? CreateAudioMeta
        {
            get => createAudioMeta;
            set
            {
                SetProperty(ref createAudioMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadRebloggedPosts
        {
            get => downloadRebloggedPosts;
            set
            {
                SetProperty(ref downloadRebloggedPosts, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? Online
        {
            get => online;
            set => SetProperty(ref online, value);
        }

        [DataMember]
        public new bool? CheckDirectoryForFiles
        {
            get => checkDirectoryForFiles;
            set
            {
                SetProperty(ref checkDirectoryForFiles, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadUrlList
        {
            get => downloadUrlList;
            set
            {
                SetProperty(ref downloadUrlList, value);
                Dirty = true;
            }
        }

        [DataMember] public new bool? Dirty { get; set; }

        [DataMember]
        public new bool? SkipGif
        {
            get => skipGif;
            set
            {
                SetProperty(ref skipGif, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? ForceSize
        {
            get => forceSize;
            set
            {
                SetProperty(ref forceSize, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? ForceRescan
        {
            get => forceRescan;
            set
            {
                SetProperty(ref forceRescan, value);
                Dirty = true;
            }
        }
    }
}
