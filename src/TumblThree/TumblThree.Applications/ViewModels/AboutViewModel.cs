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
        private readonly IApplicationUpdateService applicationUpdateService;
        private readonly DelegateCommand showWebsiteCommand;
        private readonly AsyncDelegateCommand checkForUpdatesCommand;
        private readonly DelegateCommand downloadCommand;
        private bool isCheckInProgress;
        private bool isLatestVersionAvailable;
        private string updateText;

        [ImportingConstructor]
        public AboutViewModel(IAboutView view, IApplicationUpdateService applicationUpdateService)
            : base(view)
        {
            showWebsiteCommand = new DelegateCommand(ShowWebsite);
            checkForUpdatesCommand = new AsyncDelegateCommand(CheckForUpdatesAsync);
            downloadCommand = new DelegateCommand(DownloadNewVersion);
            this.applicationUpdateService = applicationUpdateService;
        }

        public ICommand ShowWebsiteCommand
        {
            get { return showWebsiteCommand; }
        }

        public ICommand CheckForUpdatesCommand
        {
            get { return checkForUpdatesCommand; }
        }

        public ICommand DownloadCommand
        {
            get { return downloadCommand; }
        }

        public string ProductName
        {
            get { return ApplicationInfo.ProductName; }
        }

        public string Version
        {
            get { return ApplicationInfo.Version; }
        }

        public string OSVersion
        {
            get { return Environment.OSVersion.ToString(); }
        }

        public string NetVersion
        {
            get { return Environment.Version.ToString(); }
        }

        public bool Is64BitProcess
        {
            get { return Environment.Is64BitProcess; }
        }

        public bool IsCheckInProgress
        {
            get { return isCheckInProgress; }
            set { SetProperty(ref isCheckInProgress, value); }
        }

        public bool IsLatestVersionAvailable
        {
            get { return isLatestVersionAvailable; }
            set { SetProperty(ref isLatestVersionAvailable, value); }
        }

        public string UpdateText
        {
            get { return updateText; }
            set { SetProperty(ref updateText, value); }
        }

        public void ShowDialog(object owner)
        {
            ViewCore.ShowDialog(owner);
        }

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

        private void DownloadNewVersion()
        {
            Process.Start(new ProcessStartInfo(applicationUpdateService.GetDownloadUri().AbsoluteUri));
        }

        private async Task CheckForUpdatesAsync()
        {
            if (IsCheckInProgress || IsLatestVersionAvailable)
            {
                return;
            }

            IsCheckInProgress = true;
            IsLatestVersionAvailable = false;
            UpdateText = string.Empty;
            TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            await CheckForUpdatesCompleteAsync(applicationUpdateService.GetLatestReleaseFromServer());
        }

        private async Task CheckForUpdatesCompleteAsync(Task<string> task)
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
