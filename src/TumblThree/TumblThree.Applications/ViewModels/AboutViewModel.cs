using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Windows.Input;

using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class AboutViewModel : ViewModel<IAboutView>
    {
        private readonly AsyncDelegateCommand checkForUpdatesCommand;
        private readonly DelegateCommand downloadCommand;
        private readonly DelegateCommand showWebsiteCommand;

        private readonly IApplicationUpdateService applicationUpdateService;
        private bool isCheckInProgress;
        private bool isLatestVersionAvailable;
        private string updateText;

        [ImportingConstructor]
        public AboutViewModel(IAboutView view, IApplicationUpdateService applicationUpdateService)
            : base(view)
        {
            showWebsiteCommand = new DelegateCommand(ShowWebsite);
            checkForUpdatesCommand = new AsyncDelegateCommand(CheckForUpdates);
            downloadCommand = new DelegateCommand(DownloadNewVersion);
            this.applicationUpdateService = applicationUpdateService;
        }

        public ICommand ShowWebsiteCommand => showWebsiteCommand;

        public ICommand CheckForUpdatesCommand => checkForUpdatesCommand;

        public ICommand DownloadCommand => downloadCommand;

        public string ProductName => ApplicationInfo.ProductName;

        public string Version => ApplicationInfo.Version;

        public string OSVersion => Environment.OSVersion.ToString();

        public string NetVersion => Environment.Version.ToString();

        public bool Is64BitProcess => Environment.Is64BitProcess;

        public bool IsCheckInProgress
        {
            get => isCheckInProgress;
            set => SetProperty(ref isCheckInProgress, value);
        }

        public bool IsLatestVersionAvailable
        {
            get => isLatestVersionAvailable;
            set => SetProperty(ref isLatestVersionAvailable, value);
        }

        public string UpdateText
        {
            get => updateText;
            set => SetProperty(ref updateText, value);
        }

        public void ShowDialog(object owner) => ViewCore.ShowDialog(owner);

        private void ShowWebsite(object parameter)
        {
            var url = (string)parameter;
            try
            {
                Process.Start(url);
            }
            catch (Exception e)
            {
                Logger.Error("An exception occured when trying to show the url '{0}'. Exception: {1}", url, e);
            }
        }

        private void DownloadNewVersion() => Process.Start(new ProcessStartInfo(applicationUpdateService.GetDownloadUri().AbsoluteUri));

        private async Task CheckForUpdates()
        {
            if (IsCheckInProgress || IsLatestVersionAvailable)
                return;
            
            IsCheckInProgress = true;
            IsLatestVersionAvailable = false;
            UpdateText = string.Empty;
            await CheckForUpdatesComplete(applicationUpdateService.GetLatestReleaseFromServer());
        }

        private async Task CheckForUpdatesComplete(Task<string> task)
        {
            IsCheckInProgress = false;
            if (await task == null)
            {
                if (applicationUpdateService.IsNewVersionAvailable())
                {
                    UpdateText = string.Format(CultureInfo.CurrentCulture, Resources.NewVersionAvailable,
                        applicationUpdateService.GetNewAvailableVersion());
                    IsLatestVersionAvailable = true;
                }
                else
                {
                    UpdateText = string.Format(CultureInfo.CurrentCulture, Resources.ApplicationUpToDate);
                }
            }
            else
            {
                UpdateText = await task;
            }
        }
    }
}
