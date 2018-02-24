using System;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Controls;

using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for SettingsView.xaml
    /// </summary>
    [Export(typeof(IAuthenticateView))][PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class AuthenticateView : Window, IAuthenticateView
    {
        private readonly Lazy<AuthenticateViewModel> viewModel;

        public AuthenticateView()
        {
            InitializeComponent();
            viewModel = new Lazy<AuthenticateViewModel>(() => ViewHelper.GetViewModel<AuthenticateViewModel>(this));
            browser.Navigated += Browser_Navigated;
        }

        private AuthenticateViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        public void ShowDialog(object owner)
        {
            Owner = owner as Window;
            ShowDialog();
        }

        public void AddUrl(string url)
        {
            browser.Source = new Uri(url);
        }

        public string GetUrl()
        {
            return browser.Source.ToString();
        }

        private void Browser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            SetSilent(browser, true); // make it silent, no js error popus.

            try
            {
                WebBrowser wb = (WebBrowser)sender;
                if (wb.Source.ToString().Equals(ViewModel.OAuthCallbackUrl))
                {
                    Close();
                }
            }
            catch
            {
            }
        }

        public static void SetSilent(WebBrowser browser, bool silent)
        {
            if (browser == null)
            {
                throw new ArgumentNullException("browser");
            }

            // get an IWebBrowser2 from the document
            IOleServiceProvider sp = browser.Document as IOleServiceProvider;
            if (sp != null)
            {
                Guid IID_IWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
                Guid IID_IWebBrowser2 = new Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E");

                object webBrowser;
                sp.QueryService(ref IID_IWebBrowserApp, ref IID_IWebBrowser2, out webBrowser);
                if (webBrowser != null)
                {
                    webBrowser.GetType()
                              .InvokeMember("Silent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.PutDispProperty,
                                  null, webBrowser, new object[] { silent });
                }
            }
        }

        [ComImport][Guid("6D5140C1-7436-11CE-8034-00AA006009FA")][InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IOleServiceProvider
        {
            [PreserveSig]
            int QueryService([In] ref Guid guidService, [In] ref Guid riid,
                [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);
        }
    }
}
