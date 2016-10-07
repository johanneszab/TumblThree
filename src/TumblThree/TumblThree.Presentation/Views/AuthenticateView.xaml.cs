using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
   [Export(typeof(IAuthenticateView)), PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class AuthenticateView : Window, IAuthenticateView
    {

        private readonly Lazy<AuthenticateViewModel> viewModel;

        public AuthenticateView()
        {
            InitializeComponent();
            this.viewModel = new Lazy<AuthenticateViewModel>(() => ViewHelper.GetViewModel<AuthenticateViewModel>(this));

            browser.Navigated += Browser_Navigated;
        }

        private void Browser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            try
            {
                WebBrowser wb = (WebBrowser)sender;
                if (wb.Source.ToString().Contains("tumblthree"))
                {
                    this.Close();
                }
            }
            catch
            {
            }
        }

        private void Browser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {

        }

        private AuthenticateViewModel ViewModel { get { return viewModel.Value; } }

        public void ShowDialog(object owner)
        {
            Owner = owner as Window;
            ShowDialog();
        }

        private void closeWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void AddUrl(string url)
        {
            browser.Source = new Uri(url);
        }

        public string GetUrl()
        {
            return browser.Source.ToString();
        }
    }
}
