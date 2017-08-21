using System.Runtime.Serialization;

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

        public DetailsBlog()
        {
        }

        [DataMember]
        public new bool? DownloadText
        {
            get { return downloadText; }
            set
            {
                SetProperty(ref downloadText, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadQuote
        {
            get { return downloadQuote; }
            set
            {
                SetProperty(ref downloadQuote, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadPhoto
        {
            get { return downloadPhoto; }
            set
            {
                SetProperty(ref downloadPhoto, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadLink
        {
            get { return downloadLink; }
            set
            {
                SetProperty(ref downloadLink, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadAnswer
        {
            get { return downloadAnswer; }
            set
            {
                SetProperty(ref downloadAnswer, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadConversation
        {
            get { return downloadConversation; }
            set
            {
                SetProperty(ref downloadConversation, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadVideo
        {
            get { return downloadVideo; }
            set
            {
                SetProperty(ref downloadVideo, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadAudio
        {
            get { return downloadAudio; }
            set
            {
                SetProperty(ref downloadAudio, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? CreatePhotoMeta
        {
            get { return createPhotoMeta; }
            set
            {
                SetProperty(ref createPhotoMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? CreateVideoMeta
        {
            get { return createVideoMeta; }
            set
            {
                SetProperty(ref createVideoMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? CreateAudioMeta
        {
            get { return createAudioMeta; }
            set
            {
                SetProperty(ref createAudioMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadRebloggedPosts
        {
            get { return downloadRebloggedPosts; }
            set
            {
                SetProperty(ref downloadRebloggedPosts, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? Online
        {
            get { return online; }
            set { SetProperty(ref online, value); }
        }


        [DataMember]
        public new bool? CheckDirectoryForFiles
        {
            get { return checkDirectoryForFiles; }
            set
            {
                SetProperty(ref checkDirectoryForFiles, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadUrlList
        {
            get { return downloadUrlList; }
            set
            {
                SetProperty(ref downloadUrlList, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? Dirty { get; set; }

        [DataMember]
        public new bool? SkipGif
        {
            get { return skipGif; }
            set
            {
                SetProperty(ref skipGif, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? ForceSize
        {
            get { return forceSize; }
            set
            {
                SetProperty(ref forceSize, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? ForceRescan
        {
            get { return forceRescan; }
            set
            {
                SetProperty(ref forceRescan, value);
                Dirty = true;
            }
        }
    }
}
