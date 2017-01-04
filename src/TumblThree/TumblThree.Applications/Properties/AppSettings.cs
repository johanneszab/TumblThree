using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace TumblThree.Applications.Properties
{
    public sealed class AppSettings : IExtensibleDataObject
    {
        public AppSettings()
        {
            Initialize();
        }

        public static string[] imageSizes =
            new string[] {
                "1280", "500", "400", "250", "100", "75"
        };

        public static string[] videoSizes =
            new string[] {
                "1080", "480"
        };

        public static string[] blogTypes =
            new string[] {
                Resources.BlogTypesNone, Resources.BlogTypesAll, Resources.BlogTypesOnceFinished, Resources.BlogTypesNeverFinished
        };

        [DataMember]
        public string RequestTokenUrl { get; set; }

        [DataMember]
        public string AuthorizeUrl { get; set; }

        [DataMember]
        public string AccessTokenUrl { get; set; }

        [DataMember]
        public string OAuthCallbackUrl { get; set; }

        [DataMember]
        public string ApiKey { get; set; }

        [DataMember]
        public string SecretKey { get; set; }

        [DataMember]
        public string OAuthToken { get; set; }

        [DataMember]
        public string OAuthTokenSecret { get; set; }

        [DataMember]
        public double Left { get; set; }

        [DataMember]
        public double Top { get; set; }

        [DataMember]
        public double Height { get; set; }

        [DataMember]
        public double Width { get; set; }

        [DataMember]
        public bool IsMaximized { get; set; }

        [DataMember]
        public double GridSplitterPosition { get; set; }

        [DataMember]
        public string DownloadLocation { get; set; }

        [DataMember]
        public int ParallelImages { get; set; }

        [DataMember]
        public int ParallelBlogs { get; set; }

        [DataMember]
        public int TimeOut { get; set; }

        [DataMember]
        public int Bandwidth { get; set; }

        [DataMember]
        public int ImageSize { get; set; }

        public int VideoSize { get; set; }

        public string BlogType { get; set; }

        [DataMember]
        public bool CheckClipboard { get; set; }

        [DataMember]
        public bool ShowPicturePreview { get; set; }

        [DataMember]
        public bool DeleteOnlyIndex { get; set; }

        [DataMember]
        public bool CheckOnlineStatusAtStartup { get; set; }

        [DataMember]
        public bool SkipGif { get; set; }

        [DataMember]
        public bool EnablePreview { get; set; }

        [DataMember]
        public bool RemoveIndexAfterCrawl { get; set; }

        [DataMember]
        public bool DownloadImages { get; set; }

        [DataMember]
        public bool DownloadVideos { get; set; }

        [DataMember]
        public bool DownloadAudios { get; set; }

        [DataMember]
        public bool DownloadTexts { get; set; }

        [DataMember]
        public bool DownloadQuotes { get; set; }

        [DataMember]
        public bool DownloadConversations { get; set; }

        [DataMember]
        public bool DownloadLinks { get; set; }

        [DataMember]
        public bool CreateImageMeta { get; set; }

        [DataMember]
        public bool CreateVideoMeta { get; set; }

        [DataMember]
        public bool CreateAudioMeta { get; set; }

        [DataMember]
        public bool AutoDownload { get; set; }

        [DataMember]
        public string TimerInterval { get; set; }

        [DataMember]
        public bool ForceSize { get; set; }

        [DataMember]
        public Dictionary<object, Tuple<int, double>> ColumnWidths { get; set; }

        ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }

        public ObservableCollection<string> ImageSizes
        {
            get
            {
                return new ObservableCollection<string>(imageSizes);
            }
        }

        public ObservableCollection<string> VideoSizes
        {
            get
            {
                return new ObservableCollection<string>(videoSizes);
            }
        }

        public ObservableCollection<string> BlogTypes
        {
            get
            {
                return new ObservableCollection<string>(blogTypes);
            }
        }

        private void Initialize()
        {
            RequestTokenUrl = @"https://www.tumblr.com/oauth/request_token";
            AuthorizeUrl = @"https://www.tumblr.com/oauth/authorize";
            AccessTokenUrl = @"https://www.tumblr.com/oauth/access_token";
            OAuthCallbackUrl = @"https://github.com/johanneszab/TumblThree";
            ApiKey = "x8pd1InspmnuLSFKT4jNxe8kQUkbRXPNkAffntAFSk01UjRsLV";
            SecretKey = "Mul4BviRQgPLuhN1xzEqmXzwvoWicEoc4w6ftWBGWtioEvexmM";
            OAuthToken = string.Empty;
            OAuthTokenSecret = string.Empty;
            Left = 50;
            Top = 50;
            Height = 800;
            Width = 1200;
            IsMaximized = false;
            GridSplitterPosition = 250;
            DownloadLocation = @".\Blogs\";
            ParallelImages = 25;
            ParallelBlogs = 2;
            TimeOut = 120;
            Bandwidth = int.MaxValue;
            ImageSize = 1280;
            VideoSize = 1080;
            BlogType = "None";
            CheckClipboard = true;
            ShowPicturePreview = true;
            DeleteOnlyIndex = true;
            CheckOnlineStatusAtStartup = true;
            SkipGif = false;
            EnablePreview = true;
            RemoveIndexAfterCrawl = false;
            DownloadImages = true;
            DownloadVideos = true;
            DownloadTexts = true;
            DownloadAudios = true;
            DownloadQuotes = true;
            DownloadConversations = true;
            DownloadLinks = true;
            CreateImageMeta = false;
            CreateVideoMeta = false;
            CreateAudioMeta = false;
            AutoDownload = false;
            TimerInterval = "22:40:00";
            ForceSize = false;
            ColumnWidths = new Dictionary<object, Tuple<int, double>>();
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
        }
    }
}
