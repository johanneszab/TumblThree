using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Waf.Applications.Services;
using System.Windows.Input;
using TumblThree.Applications.Data;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class SettingsViewModel : ViewModel<ISettingsView>
    {
        private readonly IFolderBrowserDialog folderBrowserDialog;
        private readonly IFileDialogService fileDialogService;
        private readonly DelegateCommand authenticateCommand;
        private readonly AsyncDelegateCommand tumblrLoginCommand;
        private readonly AsyncDelegateCommand tumblrLogoutCommand;
        private readonly AsyncDelegateCommand tumblrSubmitTFACommand;
        private readonly ExportFactory<AuthenticateViewModel> authenticateViewModelFactory;
        private readonly DelegateCommand browseDownloadLocationCommand;
        private readonly DelegateCommand enableAutoDownloadCommand;
        private readonly DelegateCommand exportCommand;
        private readonly DelegateCommand browseExportLocationCommand;
        private readonly DelegateCommand saveCommand;
        private readonly FileType bloglistExportFileType;

        private readonly AppSettings settings;
        private string apiKey;
        private bool autoDownload;
        private long bandwidth;
        private string blogType;
        private bool checkClipboard;
        private bool checkDirectoryForFiles;
        private bool checkOnlineStatusOnStartup;
        private int connectionTimeInterval;
        private bool createAudioMeta;
        private bool createImageMeta;
        private bool createVideoMeta;
        private bool dumpCrawlerData;
        private string downloadPages;
        private int pageSize;
        private string downloadFrom;
        private string downloadTo;
        private string tags;
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
        private bool forceRescan;
        private string imageSize;
        private bool limitConnections;
        private bool limitScanBandwidth;
        private int maxConnections;
        private string oauthCallbackUrl;
        private string oauthToken;
        private string oauthTokenSecret;
        private int concurrentBlogs;
        private int concurrentConnections;
        private int concurrentVideoConnections;
        private int concurrentScans;
        private bool portableMode;
        private bool loadAllDatabases;
        private string proxyHost;
        private string proxyPort;
        private string proxyUsername;
        private string proxyPassword;
        private bool downloadGfycat;
        private bool downloadImgur;
        private bool downloadWebmshare;
        private bool downloadMixtape;
        private bool downloadUguu;
        private bool downloadSafeMoe;
        private bool downloadLoliSafe;
        private bool downloadCatBox;
        private MetadataType metadataFormat;
        private GfycatTypes gfycatType;
        private WebmshareTypes webmshareType;
        private MixtapeTypes mixtapeType;
        private UguuTypes uguuType;
        private SafeMoeTypes safeMoeType;
        private LoliSafeTypes loliSafeType;
        private CatBoxTypes catBoxType;
        private bool removeIndexAfterCrawl;
        private string secretKey;
        private bool showPicturePreview;
        private bool displayConfirmationDialog;
        private bool skipGif;
        private int timeOut;
        private string timerInterval;
        private int videoSize;
        private int settingsTabIndex;
        private string userAgent;
        private string tumblrUser = string.Empty;
        private string tumblrPassword = string.Empty;
        private bool tumblrLoggedIn = false;
        private bool tumblrTFADetected = false;
        private string tumblrTFAAuthCode = string.Empty;
        private string tumblrEmail = string.Empty;

        [ImportingConstructor]
        public SettingsViewModel(ISettingsView view, IShellService shellService, ICrawlerService crawlerService,
            IManagerService managerService, ILoginService loginService, IFolderBrowserDialog folderBrowserDialog, IFileDialogService fileDialogService,
            ExportFactory<AuthenticateViewModel> authenticateViewModelFactory)
            : base(view)
        {
            this.folderBrowserDialog = folderBrowserDialog;
            this.fileDialogService = fileDialogService;
            ShellService = shellService;
            settings = ShellService.Settings;
            CrawlerService = crawlerService;
            ManagerService = managerService;
            LoginService = loginService;
            this.authenticateViewModelFactory = authenticateViewModelFactory;
            browseDownloadLocationCommand = new DelegateCommand(BrowseDownloadLocation);
            browseExportLocationCommand = new DelegateCommand(BrowseExportLocation);
            authenticateCommand = new DelegateCommand(Authenticate);
            tumblrLoginCommand = new AsyncDelegateCommand(TumblrLogin);
            tumblrLogoutCommand = new AsyncDelegateCommand(TumblrLogout);
            tumblrSubmitTFACommand = new AsyncDelegateCommand(TumblrSubmitTFA);
            saveCommand = new DelegateCommand(Save);
            enableAutoDownloadCommand = new DelegateCommand(EnableAutoDownload);
            exportCommand = new DelegateCommand(ExportBlogs);
            bloglistExportFileType = new FileType(Resources.Textfile, SupportedFileTypes.BloglistExportFileType);

            Task loadSettingsTask = Load();
            view.Closed += ViewClosed;

        }

        public IShellService ShellService { get; }

        public ICrawlerService CrawlerService { get; }

        public IManagerService ManagerService { get; }

        public ILoginService LoginService { get; }

        public ICommand BrowseDownloadLocationCommand
        {
            get { return browseDownloadLocationCommand; }
        }

        public ICommand AuthenticateCommand
        {
            get { return authenticateCommand; }
        }

        public ICommand TumblrLoginCommand
        {
            get { return tumblrLoginCommand; }
        }

        public ICommand TumblrLogoutCommand
        {
            get { return tumblrLogoutCommand; }
        }

        public ICommand TumblrSubmitTFACommand
        {
            get { return tumblrSubmitTFACommand; }
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

        public int ConcurrentConnections
        {
            get { return concurrentConnections; }
            set { SetProperty(ref concurrentConnections, value); }
        }

        public int ConcurrentVideoConnections
        {
            get { return concurrentVideoConnections; }
            set { SetProperty(ref concurrentVideoConnections, value); }
        }

        public int ConcurrentBlogs
        {
            get { return concurrentBlogs; }
            set { SetProperty(ref concurrentBlogs, value); }
        }

        public int ConcurrentScans
        {
            get { return concurrentScans; }
            set { SetProperty(ref concurrentScans, value); }
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

        public bool DisplayConfirmationDialog
        {
            get { return displayConfirmationDialog; }
            set { SetProperty(ref displayConfirmationDialog, value); }
        }

        public bool DeleteOnlyIndex
        {
            get { return deleteOnlyIndex; }
            set { SetProperty(ref deleteOnlyIndex, value); }
        }

        public bool CheckOnlineStatusOnStartup
        {
            get { return checkOnlineStatusOnStartup; }
            set { SetProperty(ref checkOnlineStatusOnStartup, value); }
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

        public bool ForceRescan
        {
            get { return forceRescan; }
            set { SetProperty(ref forceRescan, value); }
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

        public bool LoadAllDatabases
        {
            get { return loadAllDatabases; }
            set { SetProperty(ref loadAllDatabases, value); }
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

        public MetadataType MetadataFormat
        {
            get { return metadataFormat; }
            set { SetProperty(ref metadataFormat, value); }
        }

        public bool DumpCrawlerData
        {
            get { return dumpCrawlerData; }
            set { SetProperty(ref dumpCrawlerData, value); }
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

        public string DownloadFrom
        {
            get { return downloadFrom; }
            set { SetProperty(ref downloadFrom, value); }
        }

        public string DownloadTo
        {
            get { return downloadTo; }
            set { SetProperty(ref downloadTo, value); }
        }

        public bool DownloadGfycat
        {
            get { return downloadGfycat; }
            set { SetProperty(ref downloadGfycat, value); }
        }

        public GfycatTypes GfycatType
        {
            get { return gfycatType; }
            set { SetProperty(ref gfycatType, value); }
        }

        public bool DownloadImgur
        {
            get { return downloadImgur; }
            set { SetProperty(ref downloadImgur, value); }
        }

        public bool DownloadWebmshare
        {
            get { return downloadWebmshare; }
            set { SetProperty(ref downloadWebmshare, value); }
        }

        public WebmshareTypes WebmshareType
        {
            get { return webmshareType; }
            set { SetProperty(ref webmshareType, value); }
        }

        public bool DownloadMixtape
        {
            get { return downloadMixtape; }
            set { SetProperty(ref downloadMixtape, value); }
        }

        public MixtapeTypes MixtapeType
        {
            get { return mixtapeType; }
            set { SetProperty(ref mixtapeType, value); }
        }

        public bool DownloadUguu
        {
            get { return downloadUguu; }
            set { SetProperty(ref downloadUguu, value); }
        }

        public UguuTypes UguuType
        {
            get { return uguuType; }
            set { SetProperty(ref uguuType, value); }
        }

        public bool DownloadSafeMoe
        {
            get { return downloadSafeMoe; }
            set { SetProperty(ref downloadSafeMoe, value); }
        }

        public SafeMoeTypes SafeMoeType
        {
            get { return safeMoeType; }
            set { SetProperty(ref safeMoeType, value); }
        }

        public bool DownloadLoliSafe
        {
            get { return downloadLoliSafe; }
            set { SetProperty(ref downloadLoliSafe, value); }
        }

        public LoliSafeTypes LoliSafeType
        {
            get { return loliSafeType; }
            set { SetProperty(ref loliSafeType, value); }
        }

        public bool DownloadCatBox
        {
            get { return downloadCatBox; }
            set { SetProperty(ref downloadCatBox, value); }
        }

        public CatBoxTypes CatBoxType
        {
            get { return catBoxType; }
            set { SetProperty(ref catBoxType, value); }
        }

        public string Tags
        {
            get { return tags; }
            set { SetProperty(ref tags, value); }
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

        public int SettingsTabIndex
        {
            get { return settingsTabIndex; }
            set { SetProperty(ref settingsTabIndex, value); }
        }

        public string UserAgent
        {
            get { return userAgent; }
            set { SetProperty(ref userAgent, value); }
        }

        public string TumblrUser
        {
            get { return tumblrUser; }
            set { SetProperty(ref tumblrUser, value); }
        }

        public string TumblrPassword
        {
            get { return tumblrPassword; }
            set { SetProperty(ref tumblrPassword, value); }
        }

        public bool TumblrLoggedIn
        {
            get { return tumblrLoggedIn; }
            set { SetProperty(ref tumblrLoggedIn, value); }
        }

        public bool TumblrTFADetected
        {
            get { return tumblrTFADetected; }
            set { SetProperty(ref tumblrTFADetected, value); }
        }

        public string TumblrTFAAuthCode
        {
            get { return tumblrTFAAuthCode; }
            set { SetProperty(ref tumblrTFAAuthCode, value); }
        }

        public string TumblrEmail
        {
            get { return tumblrEmail; }
            set { SetProperty(ref tumblrEmail, value); }
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
            folderBrowserDialog.SelectedPath = DownloadLocation;
            folderBrowserDialog.ShowNewFolderButton = true;
            if (folderBrowserDialog.ShowDialog() == true)
                DownloadLocation = folderBrowserDialog.SelectedPath;
        }

        private void BrowseExportLocation()
        {
            FileDialogResult result = fileDialogService.ShowSaveFileDialog(ShellService.ShellView, bloglistExportFileType, ExportLocation);
            if (!result.IsValid)
            {
                return;
            }

            ExportLocation = result.FileName;
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

        private async Task TumblrLogin()
        {
            try
            {
                await LoginService.PerformTumblrLogin(TumblrUser, TumblrPassword);
            }
            catch
            {
            }

            TumblrTFADetected = LoginService.CheckIfTumblrTFANeeded();
            if (!TumblrTFADetected)
            {
                await UpdateTumblrLogin();
            }
        }

        private async Task TumblrLogout()
        {
            LoginService.PerformTumblrLogout();
            await UpdateTumblrLogin();
        }

        private async Task TumblrSubmitTFA()
        {
            try
            {
                await LoginService.PerformTumblrTFALogin(TumblrUser, TumblrTFAAuthCode);
                await UpdateTumblrLogin();
            }
            catch
            {
            }
        }

        private async Task UpdateTumblrLogin()
        {
            TumblrEmail = await LoginService.GetTumblrUsername();
            if (!String.IsNullOrEmpty(TumblrEmail))
                TumblrLoggedIn = true;
            else
                TumblrLoggedIn = false;
        }

        private void CheckIfTumblrLoggedIn()
        {
            TumblrLoggedIn = LoginService.CheckIfLoggedInAsync();
        }

        public async Task Load()
        {
            LoadSettings();
            await UpdateTumblrLogin();
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
                ConcurrentConnections = settings.ConcurrentConnections;
                ConcurrentVideoConnections = settings.ConcurrentVideoConnections;
                ConcurrentBlogs = settings.ConcurrentBlogs;
                ConcurrentScans = settings.ConcurrentScans;
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
                DisplayConfirmationDialog = settings.DisplayConfirmationDialog;
                DeleteOnlyIndex = settings.DeleteOnlyIndex;
                CheckOnlineStatusOnStartup = settings.CheckOnlineStatusOnStartup;
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
                MetadataFormat = settings.MetadataFormat;
                DumpCrawlerData = settings.DumpCrawlerData;
                DownloadPages = settings.DownloadPages;
                PageSize = settings.PageSize;
                DownloadFrom = settings.DownloadFrom;
                DownloadTo = settings.DownloadTo;
                Tags = settings.Tags;
                DownloadImgur = settings.DownloadImgur;
                DownloadGfycat = settings.DownloadGfycat;
                DownloadWebmshare = settings.DownloadWebmshare;
                DownloadMixtape = settings.DownloadMixtape;
                DownloadUguu = settings.DownloadUguu;
                DownloadSafeMoe = settings.DownloadSafeMoe;
                DownloadLoliSafe = settings.DownloadLoliSafe;
                DownloadCatBox = settings.DownloadCatBox;
                GfycatType = settings.GfycatType;
                WebmshareType = settings.WebmshareType;
                MixtapeType = settings.MixtapeType;
                UguuType = settings.UguuType;
                SafeMoeType = settings.SafeMoeType;
                LoliSafeType = settings.LoliSafeType;
                CatBoxType = settings.CatBoxType;
                DownloadRebloggedPosts = settings.DownloadRebloggedPosts;
                AutoDownload = settings.AutoDownload;
                ForceSize = settings.ForceSize;
                ForceRescan = settings.ForceRescan;
                CheckDirectoryForFiles = settings.CheckDirectoryForFiles;
                DownloadUrlList = settings.DownloadUrlList;
                PortableMode = settings.PortableMode;
                LoadAllDatabases = settings.LoadAllDatabases;
                ProxyHost = settings.ProxyHost;
                ProxyPort = settings.ProxyPort;
                ProxyUsername = settings.ProxyUsername;
                ProxyPassword = settings.ProxyPassword;
                TimerInterval = settings.TimerInterval;
                SettingsTabIndex = settings.SettingsTabIndex;
                UserAgent = settings.UserAgent;
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
                ConcurrentConnections = 8;
                ConcurrentVideoConnections = 4;
                ConcurrentBlogs = 1;
                ConcurrentScans = 4;
                LimitScanBandwidth = false;
                TimeOut = 60;
                LimitConnections = true;
                MaxConnections = 90;
                ConnectionTimeInterval = 60;
                Bandwidth = 0;
                ImageSize = "raw";
                VideoSize = 1080;
                BlogType = "None";
                CheckClipboard = true;
                ShowPicturePreview = true;
                DisplayConfirmationDialog = false;
                DeleteOnlyIndex = true;
                CheckOnlineStatusOnStartup = false;
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
                MetadataFormat = MetadataType.Text;
                DumpCrawlerData = false;
                DownloadPages = string.Empty;
                PageSize = 50;
                DownloadFrom = string.Empty;
                DownloadTo = string.Empty;
                Tags = string.Empty;
                DownloadImgur = false;
                DownloadGfycat = false;
                DownloadWebmshare = false;
                DownloadMixtape = false;
                DownloadUguu = false;
                DownloadSafeMoe = false;
                DownloadLoliSafe = false;
                DownloadCatBox = false;
                GfycatType = GfycatTypes.Mp4;
                WebmshareType = WebmshareTypes.Mp4;
                MixtapeType = MixtapeTypes.Any;
                UguuType = UguuTypes.Any;
                SafeMoeType = SafeMoeTypes.Any;
                LoliSafeType = LoliSafeTypes.Any;
                CatBoxType = CatBoxTypes.Any;
                DownloadRebloggedPosts = true;
                AutoDownload = false;
                ForceSize = false;
                ForceRescan = false;
                CheckDirectoryForFiles = false;
                DownloadUrlList = false;
                PortableMode = false;
                LoadAllDatabases = false;
                ProxyHost = string.Empty;
                ProxyPort = string.Empty;
                ProxyHost = string.Empty;
                ProxyPort = string.Empty;
                TimerInterval = "22:40:00";
                SettingsTabIndex = 0;
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.99 Safari/537.36";
            }
        }

        private void Save()
        {
            bool downloadLocationChanged = DownloadLocationChanged();
            bool loadAllDatabasesChanged = LoadAllDatabasesChanged();
            SaveSettings();
            ApplySettings(downloadLocationChanged, loadAllDatabasesChanged);
        }

        private void ApplySettings(bool downloadLocationChanged, bool loadAllDatabasesChanged)
        {
            //CrawlerService.Timeconstraint.SetRate(((double)MaxConnections / (double)ConnectionTimeInterval));

            if (loadAllDatabasesChanged && downloadLocationChanged)
            {
                CrawlerService.DatabasesLoaded = new TaskCompletionSource<bool>();
                if (CrawlerService.StopCommand.CanExecute(null))
                    CrawlerService.StopCommand.Execute(null);
                CrawlerService.LoadLibraryCommand.Execute(null);
                CrawlerService.LoadAllDatabasesCommand.Execute(null);
            }
            else if (downloadLocationChanged)
            {
                CrawlerService.DatabasesLoaded = new TaskCompletionSource<bool>();
                if (CrawlerService.StopCommand.CanExecute(null))
                    CrawlerService.StopCommand.Execute(null);
                CrawlerService.LoadLibraryCommand.Execute(null);
                CrawlerService.LoadAllDatabasesCommand.Execute(null);
            }
            else if (loadAllDatabasesChanged)
            {
                CrawlerService.DatabasesLoaded = new TaskCompletionSource<bool>();
                if (CrawlerService.StopCommand.CanExecute(null))
                    CrawlerService.StopCommand.Execute(null);
                CrawlerService.LoadAllDatabasesCommand.Execute(null);
            }
        }

        private bool DownloadLocationChanged()
        {
            return !settings.DownloadLocation.Equals(DownloadLocation);
        }

        private bool LoadAllDatabasesChanged()
        {
            return !settings.LoadAllDatabases.Equals(LoadAllDatabases);
        }

        private void SaveSettings()
        {
            settings.DownloadLocation = DownloadLocation;
            settings.ExportLocation = ExportLocation;
            settings.ConcurrentConnections = ConcurrentConnections;
            settings.ConcurrentVideoConnections = ConcurrentVideoConnections;
            settings.ConcurrentBlogs = ConcurrentBlogs;
            settings.ConcurrentScans = ConcurrentScans;
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
            settings.DisplayConfirmationDialog = DisplayConfirmationDialog;
            settings.DeleteOnlyIndex = DeleteOnlyIndex;
            settings.CheckOnlineStatusOnStartup = CheckOnlineStatusOnStartup;
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
            settings.MetadataFormat = MetadataFormat;
            settings.DumpCrawlerData = DumpCrawlerData;
            settings.DownloadPages = DownloadPages;
            settings.PageSize = PageSize;
            settings.DownloadFrom = DownloadFrom;
            settings.DownloadTo = DownloadTo;
            settings.Tags = Tags;
            settings.DownloadRebloggedPosts = DownloadRebloggedPosts;
            settings.ApiKey = ApiKey;
            settings.SecretKey = SecretKey;
            settings.OAuthToken = OAuthToken;
            settings.OAuthTokenSecret = OAuthTokenSecret;
            settings.OAuthCallbackUrl = OAuthCallbackUrl;
            settings.AutoDownload = AutoDownload;
            settings.ForceSize = ForceSize;
            settings.ForceRescan = ForceRescan;
            settings.DownloadImgur = DownloadImgur;
            settings.DownloadGfycat = DownloadGfycat;
            settings.DownloadWebmshare = DownloadWebmshare;
            settings.DownloadMixtape = DownloadMixtape;
            settings.DownloadUguu = DownloadUguu;
            settings.DownloadSafeMoe = DownloadSafeMoe;
            settings.DownloadLoliSafe = DownloadLoliSafe;
            settings.DownloadCatBox = DownloadCatBox;
            settings.GfycatType = GfycatType;
            settings.WebmshareType = WebmshareType;
            settings.MixtapeType = MixtapeType;
            settings.UguuType = UguuType;
            settings.SafeMoeType = SafeMoeType;
            settings.LoliSafeType = LoliSafeType;
            settings.CatBoxType = CatBoxType;
            settings.CheckDirectoryForFiles = CheckDirectoryForFiles;
            settings.DownloadUrlList = DownloadUrlList;
            settings.PortableMode = PortableMode;
            settings.LoadAllDatabases = LoadAllDatabases;
            settings.ProxyHost = ProxyHost;
            settings.ProxyPort = ProxyPort;
            settings.ProxyUsername = ProxyUsername;
            settings.ProxyPassword = ProxyPassword;
            settings.TimerInterval = TimerInterval;
            settings.SettingsTabIndex = SettingsTabIndex;
            settings.UserAgent = UserAgent;
        }
    }
}
