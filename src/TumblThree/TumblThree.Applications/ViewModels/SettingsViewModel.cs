using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Waf.Applications;
using System.Windows.Input;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class SettingsViewModel : ViewModel<ISettingsView>
    {
        private readonly DelegateCommand authenticateCommand;
        private readonly ExportFactory<AuthenticateViewModel> authenticateViewModelFactory;
        private readonly DelegateCommand browseDownloadLocationCommand;
        private readonly DelegateCommand enableAutoDownloadCommand;
        private readonly FolderBrowserDataModel folderBrowser;
        private readonly DelegateCommand exportCommand;
        private readonly DelegateCommand browseExportLocationCommand;
        private readonly DelegateCommand saveCommand;

        private readonly AppSettings settings;
        private string apiKey;
        private bool autoDownload;
        private long bandwidth;
        private string blogType;
        private bool checkClipboard;
        private bool checkDirectoryForFiles;
        private bool checkOnlineStatusAtStartup;
        private int connectionTimeInterval;
        private bool createAudioMeta;
        private bool createImageMeta;
        private bool createVideoMeta;
        private string downloadPages;
        private int pageSize;
        private bool downloadRebloggedPosts;
        private bool deleteOnlyIndex;
        private bool downloadAudios;
        private bool downloadConversations;
        private bool downloadImages;
        private bool downloadLinks;
        private string downloadLocation;
        private string exportLocation;
        private bool downloadQuotes;
        private bool downloadTexts;
        private bool downloadAnswers;
        private bool downloadUrlList;
        private bool downloadVideos;
        private bool enablePreview;
        private bool forceSize;
        private string imageSize;
        private bool limitConnections;
        private bool limitScanBandwidth;
        private int maxConnections;
        private string oauthCallbackUrl;
        private string oauthToken;
        private string oauthTokenSecret;
        private int parallelBlogs;
        private int parallelImages;
        private int parallelScans;
        private bool portableMode;
        private string proxyHost;
        private string proxyPort;
        private string proxyUsername;
        private string proxyPassword;
        private bool removeIndexAfterCrawl;
        private string secretKey;
        private bool showPicturePreview;
        private bool skipGif;
        private int timeOut;
        private string timerInterval;
        private int videoSize;

        [ImportingConstructor]
        public SettingsViewModel(ISettingsView view, IShellService shellService, ICrawlerService crawlerService,
            IManagerService managerService, ExportFactory<AuthenticateViewModel> authenticateViewModelFactory)
            : base(view)
        {
            ShellService = shellService;
            settings = ShellService.Settings;
            CrawlerService = crawlerService;
            ManagerService = managerService;
            this.authenticateViewModelFactory = authenticateViewModelFactory;
            folderBrowser = new FolderBrowserDataModel();
            browseDownloadLocationCommand = new DelegateCommand(BrowseDownloadLocation);
            browseExportLocationCommand = new DelegateCommand(BrowseExportLocation);
            authenticateCommand = new DelegateCommand(Authenticate);
            saveCommand = new DelegateCommand(Save);
            enableAutoDownloadCommand = new DelegateCommand(EnableAutoDownload);
            exportCommand = new DelegateCommand(ExportBlogs);

            Load();
            view.Closed += ViewClosed;

            folderBrowser.PropertyChanged += FolderBrowserPropertyChanged;
        }

        public IShellService ShellService { get; }

        public ICrawlerService CrawlerService { get; }

        public IManagerService ManagerService { get; }

        public FolderBrowserDataModel FolderBrowser
        {
            get { return folderBrowser; }
        }

        public ICommand BrowseDownloadLocationCommand
        {
            get { return browseDownloadLocationCommand; }
        }

        public ICommand AuthenticateCommand
        {
            get { return authenticateCommand; }
        }

        public ICommand SaveCommand
        {
            get { return saveCommand; }
        }

        public ICommand EnableAutoDownloadCommand
        {
            get { return enableAutoDownloadCommand; }
        }

        public ICommand ExportCommand
        {
            get { return exportCommand; }
        }

        public ICommand BrowseExportLocationCommand
        {
            get { return browseExportLocationCommand; }
        }

        public string OAuthToken
        {
            get { return oauthToken; }
            set { SetProperty(ref oauthToken, value); }
        }

        public string OAuthTokenSecret
        {
            get { return oauthTokenSecret; }
            set { SetProperty(ref oauthTokenSecret, value); }
        }

        public string ApiKey
        {
            get { return apiKey; }
            set { SetProperty(ref apiKey, value); }
        }

        public string SecretKey
        {
            get { return secretKey; }
            set { SetProperty(ref secretKey, value); }
        }

        public string OAuthCallbackUrl
        {
            get { return oauthCallbackUrl; }
            set { SetProperty(ref oauthCallbackUrl, value); }
        }

        public string DownloadLocation
        {
            get { return downloadLocation; }
            set { SetProperty(ref downloadLocation, value); }
        }

        public string ExportLocation
        {
            get { return exportLocation; }
            set { SetProperty(ref exportLocation, value); }
        }

        public int ParallelImages
        {
            get { return parallelImages; }
            set { SetProperty(ref parallelImages, value); }
        }

        public int ParallelBlogs
        {
            get { return parallelBlogs; }
            set { SetProperty(ref parallelBlogs, value); }
        }

        public int ParallelScans
        {
            get { return parallelScans; }
            set { SetProperty(ref parallelScans, value); }
        }

        public int TimeOut
        {
            get { return timeOut; }
            set { SetProperty(ref timeOut, value); }
        }

        public bool LimitConnections
        {
            get { return limitConnections; }
            set { SetProperty(ref limitConnections, value); }
        }

        public int MaxConnections
        {
            get { return maxConnections; }
            set { SetProperty(ref maxConnections, value); }
        }

        public int ConnectionTimeInterval
        {
            get { return connectionTimeInterval; }
            set { SetProperty(ref connectionTimeInterval, value); }
        }

        public long Bandwidth
        {
            get { return bandwidth; }
            set { SetProperty(ref bandwidth, value); }
        }

        public bool LimitScanBandwidth
        {
            get { return limitScanBandwidth; }
            set { SetProperty(ref limitScanBandwidth, value); }
        }

        public string ImageSize
        {
            get { return imageSize; }
            set { SetProperty(ref imageSize, value); }
        }

        public int VideoSize
        {
            get { return videoSize; }
            set { SetProperty(ref videoSize, value); }
        }

        public string BlogType
        {
            get { return blogType; }
            set { SetProperty(ref blogType, value); }
        }

        public bool CheckClipboard
        {
            get { return checkClipboard; }
            set { SetProperty(ref checkClipboard, value); }
        }

        public bool ShowPicturePreview
        {
            get { return showPicturePreview; }
            set { SetProperty(ref showPicturePreview, value); }
        }

        public bool DeleteOnlyIndex
        {
            get { return deleteOnlyIndex; }
            set { SetProperty(ref deleteOnlyIndex, value); }
        }

        public bool CheckOnlineStatusAtStartup
        {
            get { return checkOnlineStatusAtStartup; }
            set { SetProperty(ref checkOnlineStatusAtStartup, value); }
        }

        public bool SkipGif
        {
            get { return skipGif; }
            set { SetProperty(ref skipGif, value); }
        }

        public bool EnablePreview
        {
            get { return enablePreview; }
            set { SetProperty(ref enablePreview, value); }
        }

        public bool AutoDownload
        {
            get { return autoDownload; }
            set { SetProperty(ref autoDownload, value); }
        }

        public bool RemoveIndexAfterCrawl
        {
            get { return removeIndexAfterCrawl; }
            set { SetProperty(ref removeIndexAfterCrawl, value); }
        }

        public bool ForceSize
        {
            get { return forceSize; }
            set { SetProperty(ref forceSize, value); }
        }

        public bool CheckDirectoryForFiles
        {
            get { return checkDirectoryForFiles; }
            set { SetProperty(ref checkDirectoryForFiles, value); }
        }

        public bool DownloadUrlList
        {
            get { return downloadUrlList; }
            set { SetProperty(ref downloadUrlList, value); }
        }

        public bool PortableMode
        {
            get { return portableMode; }
            set { SetProperty(ref portableMode, value); }
        }

        public string ProxyHost
        {
            get { return proxyHost; }
            set { SetProperty(ref proxyHost, value); }
        }

        public string ProxyPort
        {
            get { return proxyPort; }
            set { SetProperty(ref proxyPort, value); }
        }

        public string ProxyUsername
        {
            get { return proxyUsername; }
            set { SetProperty(ref proxyUsername, value); }
        }

        public string ProxyPassword
        {
            get { return proxyPassword; }
            set { SetProperty(ref proxyPassword, value); }
        }

        public bool DownloadImages
        {
            get { return downloadImages; }
            set { SetProperty(ref downloadImages, value); }
        }

        public bool DownloadVideos
        {
            get { return downloadVideos; }
            set { SetProperty(ref downloadVideos, value); }
        }

        public bool DownloadAudios
        {
            get { return downloadAudios; }
            set { SetProperty(ref downloadAudios, value); }
        }

        public bool DownloadTexts
        {
            get { return downloadTexts; }
            set { SetProperty(ref downloadTexts, value); }
        }

        public bool DownloadAnswers
        {
            get { return downloadAnswers; }
            set { SetProperty(ref downloadAnswers, value); }
        }

        public bool DownloadQuotes
        {
            get { return downloadQuotes; }
            set { SetProperty(ref downloadQuotes, value); }
        }

        public bool DownloadConversations
        {
            get { return downloadConversations; }
            set { SetProperty(ref downloadConversations, value); }
        }

        public bool DownloadLinks
        {
            get { return downloadLinks; }
            set { SetProperty(ref downloadLinks, value); }
        }

        public bool CreateImageMeta
        {
            get { return createImageMeta; }
            set { SetProperty(ref createImageMeta, value); }
        }

        public bool CreateVideoMeta
        {
            get { return createVideoMeta; }
            set { SetProperty(ref createVideoMeta, value); }
        }

        public bool CreateAudioMeta
        {
            get { return createAudioMeta; }
            set { SetProperty(ref createAudioMeta, value); }
        }

        public string DownloadPages
        {
            get { return downloadPages; }
            set { SetProperty(ref downloadPages, value); }
        }

        public int PageSize
        {
            get { return pageSize; }
            set { SetProperty(ref pageSize, value); }
        }

        public bool DownloadRebloggedPosts
        {
            get { return downloadRebloggedPosts; }
            set { SetProperty(ref downloadRebloggedPosts, value); }
        }

        public string TimerInterval
        {
            get { return timerInterval; }
            set { SetProperty(ref timerInterval, value); }
        }

        public void ShowDialog(object owner)
        {
            ViewCore.ShowDialog(owner);
        }

        private void ViewClosed(object sender, EventArgs e)
        {
            if (enableAutoDownloadCommand.CanExecute(null))
            {
                enableAutoDownloadCommand.Execute(null);
            }
        }

        private void EnableAutoDownload()
        {
            if (AutoDownload)
            {
                if (!CrawlerService.IsTimerSet)
                {
                    TimeSpan alertTime;
                    TimeSpan.TryParse(TimerInterval, out alertTime);
                    DateTime current = DateTime.Now;
                    TimeSpan timeToGo = alertTime - current.TimeOfDay;
                    if (timeToGo < TimeSpan.Zero)
                    {
                        // time already passed
                        timeToGo = timeToGo.Add(new TimeSpan(24, 00, 00));
                    }
                    CrawlerService.Timer = new Timer(x => { OnTimedEvent(); }, null, timeToGo, Timeout.InfiniteTimeSpan);

                    CrawlerService.IsTimerSet = true;
                }
            }
            else
            {
                if (CrawlerService.Timer != null)
                {
                    CrawlerService.Timer.Dispose();
                    CrawlerService.IsTimerSet = false;
                }
            }
        }

        private void ExportBlogs()
        {
            List<string> blogList = ManagerService.BlogFiles.Select(blog => blog.Url).ToList();
            blogList.Sort();
            File.WriteAllLines(ExportLocation, blogList);
        }

        private void OnTimedEvent()
        {
            if (CrawlerService.AutoDownloadCommand.CanExecute(null))
            {
                QueueOnDispatcher.CheckBeginInvokeOnUI(() => CrawlerService.AutoDownloadCommand.Execute(null));
            }
            CrawlerService.Timer.Change(new TimeSpan(24, 00, 00), Timeout.InfiniteTimeSpan);
        }

        private void BrowseDownloadLocation()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = DownloadLocation };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DownloadLocation = dialog.SelectedPath;
            }
        }

        private void BrowseExportLocation()
        {
            var dialog = new System.Windows.Forms.SaveFileDialog { FileName = exportLocation,
                Filter = string.Format(CultureInfo.CurrentCulture, Resources.ExportFileFilter),
                DefaultExt = string.Format(CultureInfo.CurrentCulture, Resources.ExportFileFilterExtension),
                AddExtension = true };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ExportLocation = dialog.FileName;
            }
        }

        private void Authenticate()
        {
            try
            {
                var url = @"https://www.tumblr.com/login";
                ShellService.Settings.OAuthCallbackUrl = "https://www.tumblr.com/dashboard";

                AuthenticateViewModel authenticateViewModel = authenticateViewModelFactory.CreateExport().Value;
                authenticateViewModel.AddUrl(url);
                authenticateViewModel.ShowDialog(ShellService.ShellView);
            }
            catch (System.Net.WebException ex)
            {
                Logger.Error("SettingsViewModel:Authenticate: {0}", ex);
                ShellService.ShowError(ex, Resources.AuthenticationFailure, ex.Message);
                return;
            }
        }

        public void Load()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (settings != null)
            {
                ApiKey = settings.ApiKey;
                SecretKey = settings.SecretKey;
                OAuthToken = settings.OAuthToken;
                OAuthTokenSecret = settings.OAuthTokenSecret;
                OAuthCallbackUrl = settings.OAuthCallbackUrl;
                DownloadLocation = settings.DownloadLocation;
                ExportLocation = settings.ExportLocation;
                ParallelImages = settings.ParallelImages;
                ParallelBlogs = settings.ParallelBlogs;
                ParallelScans = settings.ParallelScans;
                LimitScanBandwidth = settings.LimitScanBandwidth;
                ImageSize = settings.ImageSize;
                VideoSize = settings.VideoSize;
                BlogType = settings.BlogType;
                TimeOut = settings.TimeOut;
                LimitConnections = settings.LimitConnections;
                MaxConnections = settings.MaxConnections;
                connectionTimeInterval = settings.ConnectionTimeInterval;
                Bandwidth = settings.Bandwidth;
                CheckClipboard = settings.CheckClipboard;
                ShowPicturePreview = settings.ShowPicturePreview;
                DeleteOnlyIndex = settings.DeleteOnlyIndex;
                CheckOnlineStatusAtStartup = settings.CheckOnlineStatusAtStartup;
                SkipGif = settings.SkipGif;
                EnablePreview = settings.EnablePreview;
                RemoveIndexAfterCrawl = settings.RemoveIndexAfterCrawl;
                DownloadImages = settings.DownloadImages;
                DownloadVideos = settings.DownloadVideos;
                DownloadTexts = settings.DownloadTexts;
                DownloadAnswers = settings.DownloadAnswers;
                DownloadAudios = settings.DownloadAudios;
                DownloadConversations = settings.DownloadConversations;
                DownloadLinks = settings.DownloadLinks;
                DownloadQuotes = settings.DownloadQuotes;
                CreateImageMeta = settings.CreateImageMeta;
                CreateVideoMeta = settings.CreateVideoMeta;
                CreateAudioMeta = settings.CreateAudioMeta;
                DownloadPages = settings.DownloadPages;
                PageSize = settings.PageSize;
                DownloadRebloggedPosts = settings.DownloadRebloggedPosts;
                AutoDownload = settings.AutoDownload;
                ForceSize = settings.ForceSize;
                CheckDirectoryForFiles = settings.CheckDirectoryForFiles;
                DownloadUrlList = settings.DownloadUrlList;
                PortableMode = settings.PortableMode;
                ProxyHost = settings.ProxyHost;
                ProxyPort = settings.ProxyPort;
                ProxyUsername = settings.ProxyUsername;
                ProxyPassword = settings.ProxyPassword;
                TimerInterval = settings.TimerInterval;
            }
            else
            {
                ApiKey = "x8pd1InspmnuLSFKT4jNxe8kQUkbRXPNkAffntAFSk01UjRsLV";
                SecretKey = "Mul4BviRQgPLuhN1xzEqmXzwvoWicEoc4w6ftWBGWtioEvexmM";
                OAuthCallbackUrl = @"https://github.com/johanneszab/TumblThree";
                OAuthToken = string.Empty;
                OAuthTokenSecret = string.Empty;
                DownloadLocation = "Blogs";
                ExportLocation = "blogs.txt";
                ParallelImages = 8;
                ParallelBlogs = 1;
                ParallelScans = 4;
                LimitScanBandwidth = false;
                TimeOut = 120;
                LimitConnections = true;
                MaxConnections = 90;
                ConnectionTimeInterval = 60;
                Bandwidth = 0;
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
                DownloadAudios = true;
                DownloadTexts = true;
                DownloadAnswers = true;
                DownloadConversations = true;
                DownloadQuotes = true;
                DownloadLinks = true;
                CreateImageMeta = false;
                CreateVideoMeta = false;
                CreateAudioMeta = false;
                DownloadPages = String.Empty;
                PageSize = 50;
                DownloadRebloggedPosts = true;
                AutoDownload = false;
                ForceSize = false;
                CheckDirectoryForFiles = false;
                DownloadUrlList = false;
                PortableMode = false;
                ProxyHost = string.Empty;
                ProxyPort = string.Empty;
                ProxyHost = string.Empty;
                ProxyPort = string.Empty;
                TimerInterval = "22:40:00";
            }
        }

        private void Save()
        {
            bool downloadLocationChanged = DownloadLocationChanged();
            SaveSettings();
            ApplySettings(downloadLocationChanged);
        }

        private void ApplySettings(bool downloadLocationChanged)
        {
            CrawlerService.Timeconstraint.SetRate(((double)MaxConnections / (double)ConnectionTimeInterval));
            
            if (!CrawlerService.IsCrawl && !downloadLocationChanged)
            {
                CrawlerService.LoadLibraryCommand.Execute(null);
            }
        }

        private bool DownloadLocationChanged()
        {
            return settings.DownloadLocation.Equals(DownloadLocation);
        }

        private void SaveSettings()
        {
            settings.DownloadLocation = DownloadLocation;
            settings.ExportLocation = ExportLocation;
            settings.ParallelImages = ParallelImages;
            settings.ParallelBlogs = ParallelBlogs;
            settings.ParallelScans = ParallelScans;
            settings.LimitScanBandwidth = LimitScanBandwidth;
            settings.TimeOut = TimeOut;
            settings.LimitConnections = LimitConnections;
            settings.MaxConnections = MaxConnections;
            settings.ConnectionTimeInterval = ConnectionTimeInterval;
            settings.Bandwidth = Bandwidth;
            settings.ImageSize = ImageSize;
            settings.VideoSize = VideoSize;
            settings.BlogType = BlogType;
            settings.CheckClipboard = CheckClipboard;
            settings.ShowPicturePreview = ShowPicturePreview;
            settings.DeleteOnlyIndex = DeleteOnlyIndex;
            settings.CheckOnlineStatusAtStartup = CheckOnlineStatusAtStartup;
            settings.SkipGif = SkipGif;
            settings.EnablePreview = EnablePreview;
            settings.RemoveIndexAfterCrawl = RemoveIndexAfterCrawl;
            settings.DownloadImages = DownloadImages;
            settings.DownloadVideos = DownloadVideos;
            settings.DownloadTexts = DownloadTexts;
            settings.DownloadAnswers = DownloadAnswers;
            settings.DownloadAudios = DownloadAudios;
            settings.DownloadConversations = DownloadConversations;
            settings.DownloadQuotes = DownloadQuotes;
            settings.DownloadLinks = DownloadLinks;
            settings.CreateImageMeta = CreateImageMeta;
            settings.CreateVideoMeta = CreateVideoMeta;
            settings.CreateAudioMeta = CreateAudioMeta;
            settings.DownloadPages = DownloadPages;
            settings.PageSize = PageSize;
            settings.DownloadRebloggedPosts = DownloadRebloggedPosts;
            settings.ApiKey = ApiKey;
            settings.SecretKey = SecretKey;
            settings.OAuthToken = OAuthToken;
            settings.OAuthTokenSecret = OAuthTokenSecret;
            settings.OAuthCallbackUrl = OAuthCallbackUrl;
            settings.AutoDownload = AutoDownload;
            settings.ForceSize = ForceSize;
            settings.CheckDirectoryForFiles = CheckDirectoryForFiles;
            settings.DownloadUrlList = DownloadUrlList;
            settings.PortableMode = PortableMode;
            settings.ProxyHost = ProxyHost;
            settings.ProxyPort = ProxyPort;
            settings.ProxyUsername = ProxyUsername;
            settings.ProxyPassword = ProxyPassword;
            settings.TimerInterval = TimerInterval;
        }

        private void FolderBrowserPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FolderBrowserDataModel.BlogPath))
            {
                settings.DownloadLocation = e.PropertyName;
            }
        }
    }
}
