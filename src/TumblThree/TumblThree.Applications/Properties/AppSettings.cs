using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using System.Windows;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Properties
{
    [Export(typeof(AppSettings))]
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

        private static string[] tumblrHosts =
            new string[]
            {
                        "data.tumblr.com"
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
        public int ConcurrentConnections { get; set; }

        [DataMember]
        public int ConcurrentVideoConnections { get; set; }

        [DataMember]
        public int ConcurrentBlogs { get; set; }

        [DataMember]
        public int ConcurrentScans { get; set; }

        [DataMember]
        public bool LimitScanBandwidth { get; set; }

        [DataMember]
        public int TimeOut { get; set; }

        [DataMember]
        public double ProgessUpdateInterval { get; set; }

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
        public bool DisplayConfirmationDialog { get; set; }

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
        public string DownloadFrom { get; set; }

        [DataMember]
        public string DownloadTo { get; set; }

        [DataMember]
        public string Tags { get; set; }

        [DataMember]
        public bool CreateImageMeta { get; set; }

        [DataMember]
        public bool CreateVideoMeta { get; set; }

        [DataMember]
        public bool CreateAudioMeta { get; set; }

        [DataMember]
        public MetadataType MetadataFormat { get; set; }

        [DataMember]
        public bool DumpCrawlerData { get; set; }

        [DataMember]
        public bool DownloadRebloggedPosts { get; set; }

        [DataMember]
        public bool DownloadGfycat { get; set; }

        [DataMember]
        public GfycatTypes GfycatType { get; set; }

        [DataMember]
        public bool DownloadImgur { get; set; }

        [DataMember]
        public bool DownloadWebmshare { get; set; }

        [DataMember]
        public WebmshareTypes WebmshareType { get; set; }

        [DataMember]
        public bool DownloadMixtape { get; set; }

        [DataMember]
        public MixtapeTypes MixtapeType { get; set; }

        [DataMember]
        public bool DownloadUguu { get; set; }

        [DataMember]
        public UguuTypes UguuType { get; set; }

        [DataMember]
        public bool DownloadSafeMoe { get; set; }

        [DataMember]
        public SafeMoeTypes SafeMoeType { get; set; }

        [DataMember]
        public bool DownloadLoliSafe { get; set; }

        [DataMember]
        public LoliSafeTypes LoliSafeType { get; set; }

        [DataMember]
        public bool DownloadCatBox { get; set; }

        [DataMember]
        public CatBoxTypes CatBoxType { get; set; }

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
        public bool LoadAllDatabases { get; set; }

        [DataMember]
        public string ProxyHost { get; set; }

        [DataMember]
        public string ProxyPort { get; set; }

        [DataMember]
        public string ProxyUsername { get; set; }

        [DataMember]
        public string ProxyPassword { get; set; }

        [DataMember]
        public int SettingsTabIndex { get; set; }

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

        public string[] TumblrHosts
        {
            get { return tumblrHosts; }
            set { tumblrHosts = value; }
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
            ConcurrentConnections = 8;
            ConcurrentVideoConnections = 4;
            ConcurrentBlogs = 1;
            ConcurrentScans = 4;
            LimitScanBandwidth = false;
            TimeOut = 30;
            LimitConnections = true;
            MaxConnections = 90;
            ConnectionTimeInterval = 60;
            MaxNumberOfRetries = 10;
            ProgessUpdateInterval = 200;
            Bandwidth = 0;
            BufferSize = 512;
            ImageSize = "raw";
            VideoSize = 1080;
            BlogType = "None";
            CheckClipboard = true;
            ShowPicturePreview = true;
            DisplayConfirmationDialog = false;
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
            MetadataFormat = MetadataType.Text;
            PageSize = 50;
            DownloadRebloggedPosts = true;
            AutoDownload = false;
            TimerInterval = "22:40:00";
            ForceSize = false;
            CheckDirectoryForFiles = false;
            DownloadUrlList = false;
            PortableMode = false;
            LoadAllDatabases = false;
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
