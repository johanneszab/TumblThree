using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Windows;

namespace TumblThree.Applications.Properties
{
    public sealed class AppSettings : IExtensibleDataObject
    {
        private static readonly string[] blogTypes =
            new string[]
            {
                Resources.BlogTypesNone, Resources.BlogTypesAll, Resources.BlogTypesOnceFinished, Resources.BlogTypesNeverFinished
            };

        private static readonly string[] imageSizes =
            new string[]
            {
                "raw", "1280", "500", "400", "250", "100", "75"
            };

        private static readonly string[] videoSizes =
            new string[]
            {
                "1080", "480"
            };

        private static readonly string[] tumblrHosts =
            new string[]
            {
                        "media.tumblr.com"
            };

        public AppSettings()
        {
            Initialize();
        }

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
        public string ExportLocation { get; set; }

        [DataMember]
        public int ParallelImages { get; set; }

        [DataMember]
        public int ParallelBlogs { get; set; }

        [DataMember]
        public int ParallelScans { get; set; }

        [DataMember]
        public bool LimitScanBandwidth { get; set; }

        [DataMember]
        public int TimeOut { get; set; }

        [DataMember]
        public bool LimitConnections { get; set; }

        [DataMember]
        public int MaxConnections { get; set; }

        [DataMember]
        public int ConnectionTimeInterval { get; set; }

        [DataMember]
        public int MaxNumberOfRetries { get; set; }

        [DataMember]
        public long Bandwidth { get; set; }

        [DataMember]
        public int BufferSize { get; set; }

        [DataMember]
        public string ImageSize { get; set; }

        [DataMember]
        public int VideoSize { get; set; }

        [DataMember]
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
        public bool DownloadAnswers { get; set; }

        [DataMember]
        public bool DownloadLinks { get; set; }

        [DataMember]
        public string DownloadPages { get; set; }

        [DataMember]
        public int PageSize { get; set; }

        [DataMember]
        public bool CreateImageMeta { get; set; }

        [DataMember]
        public bool CreateVideoMeta { get; set; }

        [DataMember]
        public bool CreateAudioMeta { get; set; }

        [DataMember]
        public bool DownloadRebloggedPosts { get; set; }

        [DataMember]
        public bool AutoDownload { get; set; }

        [DataMember]
        public string TimerInterval { get; set; }

        [DataMember]
        public bool ForceSize { get; set; }

        [DataMember]
        public bool CheckDirectoryForFiles { get; set; }

        [DataMember]
        public bool DownloadUrlList { get; set; }

        [DataMember]
        public bool PortableMode { get; set; }

        [DataMember]
        public string ProxyHost { get; set; }

        [DataMember]
        public string ProxyPort { get; set; }

        [DataMember]
        public string ProxyUsername { get; set; }

        [DataMember]
        public string ProxyPassword { get; set; }

        [DataMember]
        public Dictionary<object, Tuple<int, double, Visibility>> ColumnSettings { get; set; }

        public ObservableCollection<string> ImageSizes
        {
            get { return new ObservableCollection<string>(imageSizes); }
        }

        public ObservableCollection<string> VideoSizes
        {
            get { return new ObservableCollection<string>(videoSizes); }
        }

        public ObservableCollection<string> BlogTypes
        {
            get { return new ObservableCollection<string>(blogTypes); }
        }

        public ObservableCollection<string> TumblrHosts
        {
            get { return new ObservableCollection<string>(tumblrHosts); }
        }

        ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }

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
            DownloadLocation = @"Blogs";
            ExportLocation = @"blogs.txt";
            ParallelImages = 25;
            ParallelBlogs = 2;
            ParallelScans = 4;
            LimitScanBandwidth = false;
            TimeOut = 120;
            LimitConnections = true;
            MaxConnections = 90;
            ConnectionTimeInterval = 60;
            MaxNumberOfRetries = 10;
            Bandwidth = 0;
            BufferSize = 512;
            ImageSize = "raw";
            VideoSize = 1080;
            BlogType = "None";
            CheckClipboard = true;
            ShowPicturePreview = true;
            DeleteOnlyIndex = true;
            CheckOnlineStatusAtStartup = false;
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
            DownloadAnswers = true;
            CreateImageMeta = false;
            CreateVideoMeta = false;
            CreateAudioMeta = false;
            PageSize = 50;
            DownloadRebloggedPosts = true;
            AutoDownload = false;
            TimerInterval = "22:40:00";
            ForceSize = false;
            CheckDirectoryForFiles = false;
            DownloadUrlList = false;
            PortableMode = false;
            ProxyHost = string.Empty;
            ProxyPort = string.Empty;
            ProxyUsername = string.Empty;
            ProxyPassword = string.Empty;
            ColumnSettings = new Dictionary<object, Tuple<int, double, Visibility>>();
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
        }
    }
}
