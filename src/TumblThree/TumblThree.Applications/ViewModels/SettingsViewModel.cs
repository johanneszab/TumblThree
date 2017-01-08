using System;
using System.Waf.Applications;
using TumblThree.Applications.Views;
using System.ComponentModel.Composition;
using TumblThree.Applications.Services;
using TumblThree.Applications.Properties;
using TumblThree.Applications.DataModels;
using System.ComponentModel;
using System.Windows.Input;
using System.Text.RegularExpressions;
using TumblThree.Domain;
using System.Threading;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class SettingsViewModel : ViewModel<ISettingsView>
    {

        private readonly AppSettings settings;
        private readonly FolderBrowserDataModel folderBrowser;
        private DelegateCommand displayFolderBrowserCommand;
        private DelegateCommand authenticateCommand;
        private DelegateCommand enableAutoDownloadCommand;
        private readonly ExportFactory<AuthenticateViewModel> authenticateViewModelFactory;
        private string downloadLocation;
        private string apiKey;
        private string secretKey;
        private string oauthToken;
        private string oauthTokenSecret;
        private string oauthCallbackUrl;
        private int parallelImages;
        private int parallelBlogs;
        private int timeOut;
        private int bandwidth;
        private int imageSize;
        private int videoSize;
        private string blogType;
        private bool checkClipboard;
        private bool showPicturePreview;
        private bool deleteOnlyIndex;
        private bool checkOnlineStatusAtStartup;
        private bool skipGif;
        private bool enablePreview;
        private bool autoDownload;
        private bool removeIndexAfterCrawl;
        private bool forceSize;
        private string proxyHost;
        private string proxyPort;
        private bool downloadImages;
        private bool downloadVideos;
        private bool downloadAudios;
        private bool downloadTexts;
        private bool downloadConversations;
        private bool downloadQuotes;
        private bool downloadLinks;
        private bool createImageMeta;
        private bool createVideoMeta;
        private bool createAudioMeta;
        private string timerInterval;

        //private bool isloaded = false;

        [ImportingConstructor]
        public SettingsViewModel(ISettingsView view, IShellService shellService, CrawlerService crawlerService, ExportFactory<AuthenticateViewModel> authenticateViewModelFactory)
            : base(view)
        {
            ShellService = shellService;
            settings = ShellService.Settings;
            CrawlerService = crawlerService;
            this.authenticateViewModelFactory = authenticateViewModelFactory;
            this.folderBrowser = new FolderBrowserDataModel();
            this.displayFolderBrowserCommand = new DelegateCommand(DisplayFolderBrowser);
            this.authenticateCommand = new DelegateCommand(Authenticate);
            this.enableAutoDownloadCommand = new DelegateCommand(EnableAutoDownload);

            Load();
            view.Closed += ViewClosed;

            folderBrowser.PropertyChanged += FolderBrowserPropertyChanged;
        }

        public IShellService ShellService { get; }

        public CrawlerService CrawlerService { get; }

        public FolderBrowserDataModel FolderBrowser { get { return folderBrowser; } }

        public void ShowDialog(object owner)
        {
            ViewCore.ShowDialog(owner);
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

        public int TimeOut
        {
            get { return timeOut; }
            set { SetProperty(ref timeOut, value); }
        }

        public int Bandwidth
        {
            get { return bandwidth; }
            set { SetProperty(ref bandwidth, value); }
        }

        public int ImageSize
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

        public string TimerInterval
        {
            get { return timerInterval; }
            set { SetProperty(ref timerInterval, value); }
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
                ParallelImages = settings.ParallelImages;
                ParallelBlogs = settings.ParallelBlogs;
                ImageSize = settings.ImageSize;
                VideoSize = settings.VideoSize;
                BlogType = settings.BlogType;
                TimeOut = settings.TimeOut;
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
                DownloadAudios = settings.DownloadAudios;
                DownloadConversations = settings.DownloadConversations;
                DownloadLinks = settings.DownloadLinks;
                DownloadQuotes = settings.DownloadQuotes;
                CreateImageMeta = settings.CreateImageMeta;
                CreateVideoMeta = settings.CreateVideoMeta;
                CreateAudioMeta = settings.CreateAudioMeta;
                AutoDownload = settings.AutoDownload;
                ForceSize = settings.ForceSize;
                ProxyHost = settings.ProxyHost;
                ProxyPort = settings.ProxyPort;
                TimerInterval = settings.TimerInterval;
            }
            else
            {
                ApiKey = "x8pd1InspmnuLSFKT4jNxe8kQUkbRXPNkAffntAFSk01UjRsLV";
                SecretKey = "Mul4BviRQgPLuhN1xzEqmXzwvoWicEoc4w6ftWBGWtioEvexmM";
                OAuthCallbackUrl = @"https://github.com/johanneszab/TumblThree";
                OAuthToken = string.Empty;
                OAuthTokenSecret = string.Empty;
                DownloadLocation = ".\\Blogs";
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
                DownloadAudios = true;
                DownloadTexts = true;
                DownloadConversations = true;
                DownloadQuotes = true;
                DownloadLinks = true;
                CreateImageMeta = false;
                CreateVideoMeta = false;
                CreateAudioMeta = false;
                AutoDownload = false;
                ForceSize = false;
                ProxyHost = String.Empty;
                ProxyPort = String.Empty;
                TimerInterval = "22:40:00";
            }
        }

        private void ViewClosed(object sender, EventArgs e)
        {
            SaveSettings();
            if (enableAutoDownloadCommand.CanExecute(null))
            {
                enableAutoDownloadCommand.Execute(null);
            }
        }
        private void SaveSettings()
        {
            settings.DownloadLocation = DownloadLocation;
            settings.ParallelImages = ParallelImages;
            settings.ParallelBlogs = ParallelBlogs;
            settings.TimeOut = TimeOut;
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
            settings.DownloadAudios = DownloadAudios;
            settings.DownloadConversations = DownloadConversations;
            settings.DownloadQuotes = DownloadQuotes;
            settings.DownloadLinks = DownloadLinks;
            settings.CreateImageMeta = CreateImageMeta;
            settings.CreateVideoMeta = CreateVideoMeta;
            settings.CreateAudioMeta = CreateAudioMeta;
            settings.ApiKey = ApiKey;
            settings.SecretKey = SecretKey;
            settings.OAuthToken = OAuthToken;
            settings.OAuthTokenSecret = OAuthTokenSecret;
            settings.OAuthCallbackUrl = OAuthCallbackUrl;
            settings.AutoDownload = AutoDownload;
            settings.ForceSize = ForceSize;
            settings.ProxyHost = ProxyHost;
            settings.ProxyPort = ProxyPort;
            settings.TimerInterval = TimerInterval;
        }

        public void Load()
        {
            LoadSettings();
        }

        public ICommand DisplayFolderBrowserCommand { get { return displayFolderBrowserCommand; } }

        public ICommand AuthenticateCommand { get { return authenticateCommand; } }

        public ICommand EnableAutoDownloadCommand { get { return enableAutoDownloadCommand; } }

        private void EnableAutoDownload()
        {
            if (AutoDownload)
            {
                if (CrawlerService.IsTimerSet == false)
                {
                    TimeSpan alertTime;
                    TimeSpan.TryParse(settings.TimerInterval, out alertTime);
                    DateTime current = DateTime.Now;
                    TimeSpan timeToGo = alertTime - current.TimeOfDay;
                    if (timeToGo < TimeSpan.Zero)
                    {
                        // time already passed
                        timeToGo = timeToGo.Add(new TimeSpan(24, 00, 00));
                    }
                    CrawlerService.Timer = new System.Threading.Timer(x =>
                    {
                        this.OnTimedEvent();
                    }, null, timeToGo, Timeout.InfiniteTimeSpan);

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

        private void OnTimedEvent()
        {
            if (CrawlerService.AutoDownloadCommand.CanExecute(null))
                System.Windows.Application.Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() => {
                        CrawlerService.AutoDownloadCommand.Execute(null);
                    }));
            CrawlerService.Timer.Change(new TimeSpan(24, 00, 00), Timeout.InfiniteTimeSpan);
        }

        private void DisplayFolderBrowser()
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = DownloadLocation;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DownloadLocation = dialog.SelectedPath;
            }
        }

        private void Authenticate()
        {
            try
            {
                ShellService.OAuthManager["consumer_key"] = ApiKey;
                ShellService.OAuthManager["consumer_secret"] = SecretKey;
                OAuthResponse requestToken =
                    ShellService.OAuthManager.AcquireRequestToken(settings.RequestTokenUrl, "POST");
                var url = settings.AuthorizeUrl + @"?oauth_token=" + ShellService.OAuthManager["token"];

                var authenticateViewModel = authenticateViewModelFactory.CreateExport().Value;
                authenticateViewModel.AddUrl(url);
                authenticateViewModel.ShowDialog(ShellService.ShellView);
                string oauthTokenUrl = authenticateViewModel.GetUrl();

                Regex regex = new Regex("oauth_verifier=(.*)");
                string oauthVerifer = regex.Match(oauthTokenUrl).Groups[1].ToString();

                //FIXME: 401 (Unauthorized): "oauth_signature does not match expected value"
                OAuthResponse accessToken =
                    ShellService.OAuthManager.AcquireAccessToken(settings.AccessTokenUrl, "POST", oauthVerifer);

                regex = new Regex("oauth_token=(.*)&oauth_token_secret");
                OAuthToken = regex.Match(accessToken.AllText).Groups[1].ToString();

                regex = new Regex("oauth_token_secret=(.*)");
                OAuthTokenSecret = regex.Match(accessToken.AllText).Groups[1].ToString();

                ShellService.OAuthManager["token"] = OAuthToken;
                ShellService.OAuthManager["token_secret"] = OAuthTokenSecret;
            }
            catch (System.Net.WebException ex)
            {
                Logger.Error("SettingsViewModel:Authenticate: {0}", ex);
                ShellService.ShowError(ex, Resources.AuthenticationFailure, ex.Message);
                return;
            }
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
