using System;
using System.Collections.Generic;
using System.Waf.Applications;
using TumblThree.Applications.Views;
using TumblThree.Domain;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using TumblThree.Applications.Services;
using TumblThree.Applications.Properties;
using TumblThree.Applications.DataModels;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class SettingsViewModel : ViewModel<ISettingsView>
    {

        private readonly AppSettings settings;
        private readonly FolderBrowserDataModel folderBrowser;
        private DelegateCommand displayFolderBrowserCommand;
        private DelegateCommand authenticateCommand;
        private string downloadLocation;
        private int parallelImages;
        private int parallelBlogs;
        private int imageSize;
        private int videoSize;
        private bool checkClipboard;
        private bool showPicturePreview;
        private bool deleteOnlyIndex;
        private bool checkOnlineStatusAtStartup;
        private bool skipGif;
        private bool removeIndexAfterCrawl;
        private bool downloadImages;
        private bool downloadVideos;

        //private bool isloaded = false;

        [ImportingConstructor]
        public SettingsViewModel(ISettingsView view, IShellService shellService)
            : base(view)
        {
            ShellService = shellService;
            settings = ShellService.Settings;
            this.folderBrowser = new FolderBrowserDataModel();
            this.displayFolderBrowserCommand = new DelegateCommand(DisplayFolderBrowser);
            this.authenticateCommand = new DelegateCommand(Authenticate);

            Load();
            view.Closed += ViewClosed;

            folderBrowser.PropertyChanged += FolderBrowserPropertyChanged;
        }

        public IShellService ShellService { get; }

        public FolderBrowserDataModel FolderBrowser { get { return folderBrowser; } }

        public void ShowDialog(object owner)
        {
            ViewCore.ShowDialog(owner);
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

        public bool RemoveIndexAfterCrawl
        {
            get { return removeIndexAfterCrawl; }
            set { SetProperty(ref removeIndexAfterCrawl, value); }
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

        private void LoadSettings()
        {
            if (settings != null)
            {
                DownloadLocation = settings.DownloadLocation;
                ParallelImages = settings.ParallelImages;
                ParallelBlogs = settings.ParallelBlogs;
                ImageSize = settings.ImageSize;
                VideoSize = settings.VideoSize;
                CheckClipboard = settings.CheckClipboard;
                ShowPicturePreview = settings.ShowPicturePreview;
                DeleteOnlyIndex = settings.DeleteOnlyIndex;
                CheckOnlineStatusAtStartup = settings.CheckOnlineStatusAtStartup;
                SkipGif = settings.SkipGif;
                RemoveIndexAfterCrawl = settings.RemoveIndexAfterCrawl;
                DownloadImages = settings.DownloadImages;
                DownloadVideos = settings.DownloadVideos;
            }
            else
            {
                DownloadLocation = ".\\Blogs";
                ParallelImages = 25;
                ParallelBlogs = 2;
                ImageSize = 1280;
                VideoSize = 1080;
                CheckClipboard = true;
                ShowPicturePreview = true;
                DeleteOnlyIndex = true;
                CheckOnlineStatusAtStartup = true;
                SkipGif = false;
                RemoveIndexAfterCrawl = false;
                DownloadImages = true;
                DownloadVideos = false;
            }
        }

        private void ViewClosed(object sender, EventArgs e)
        {
            SaveSettings();
        }
        private void SaveSettings()
        {
            settings.DownloadLocation = DownloadLocation;
            settings.ParallelImages = ParallelImages;
            settings.ParallelBlogs = ParallelBlogs;
            settings.ImageSize = ImageSize;
            settings.VideoSize = VideoSize;
            settings.CheckClipboard = CheckClipboard;
            settings.ShowPicturePreview = ShowPicturePreview;
            settings.DeleteOnlyIndex = DeleteOnlyIndex;
            settings.CheckOnlineStatusAtStartup = CheckOnlineStatusAtStartup;
            settings.SkipGif = SkipGif;
            settings.RemoveIndexAfterCrawl = RemoveIndexAfterCrawl;
            settings.DownloadImages = DownloadImages;
            settings.DownloadVideos = DownloadVideos;
        }

        public void Load()
        {
            LoadSettings();
        }

        public ICommand DisplayFolderBrowserCommand { get { return displayFolderBrowserCommand; } }

        public ICommand AuthenticateCommand { get { return authenticateCommand; } }

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
            OAuthManager oauthManager = new OAuthManager();
            oauthManager["consumer_key"] = settings.ApiKey;
            oauthManager["consumer_secret"] = settings.SecretKey;
            //oauthManager["callback"] = Uri.EscapeUriString(shellService.Settings.OAuthCallback);
            OAuthResponse requestToken =
                oauthManager.AcquireRequestToken("https://www.tumblr.com/oauth/request_token", "GET");
            // Start the browser to get the access token from the user
            var url = @"https://www.tumblr.com/oauth/authorize?oauth_token=" + oauthManager["token"];

            System.Diagnostics.Process.Start(url);

            //System.Windows.Controls.WebBrowser browser = new System.Windows.Controls.WebBrowser();
            //browser.Source = new Uri(url);
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
