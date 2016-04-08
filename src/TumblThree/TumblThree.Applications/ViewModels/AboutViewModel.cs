using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Waf.Applications;
using System.Windows.Input;
using TumblThree.Applications.Views;
using TumblThree.Domain;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class AboutViewModel : ViewModel<IAboutView>
    {
        private readonly DelegateCommand showWebsiteCommand;


        [ImportingConstructor]
        public AboutViewModel(IAboutView view)
            : base(view)
        {
            showWebsiteCommand = new DelegateCommand(ShowWebsite);
        }


        public ICommand ShowWebsiteCommand { get { return showWebsiteCommand; } }

        public string ProductName { get { return ApplicationInfo.ProductName; } }

        public string Version { get { return ApplicationInfo.Version; } }

        public string OSVersion { get { return Environment.OSVersion.ToString(); } }

        public string NetVersion { get { return Environment.Version.ToString(); } }

        public bool Is64BitProcess { get { return Environment.Is64BitProcess; } }


        public void ShowDialog(object owner)
        {
            ViewCore.ShowDialog(owner);
        }

        private void ShowWebsite(object parameter)
        {
            string url = (string)parameter;
            try
            {
                Process.Start(url);
            }
            catch (Exception e)
            {
                Logger.Error("An exception occured when trying to show the url '{0}'. Exception: {1}", url, e);
            }
        }
    }
}
