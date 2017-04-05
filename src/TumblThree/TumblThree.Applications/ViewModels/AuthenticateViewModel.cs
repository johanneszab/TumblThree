using System;
using System.ComponentModel.Composition;
using System.Waf.Applications;

using TumblThree.Applications.Services;
using TumblThree.Applications.Views;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class AuthenticateViewModel : ViewModel<IAuthenticateView>
    {
        private string oauthCallbackUrl;

        [ImportingConstructor]
        public AuthenticateViewModel(IAuthenticateView view, IShellService shellService)
            : base(view)
        {
            view.Closed += ViewClosed;
            ShellService = shellService;
            oauthCallbackUrl = shellService.Settings.OAuthCallbackUrl;
        }

        public IShellService ShellService { get; }

        public string OAuthCallbackUrl
        {
            get { return oauthCallbackUrl; }
            set { SetProperty(ref oauthCallbackUrl, value); }
        }

        //public IView View { get; set; }

        public void ShowDialog(object owner)
        {
            ViewCore.ShowDialog(owner);
        }

        private void ViewClosed(object sender, EventArgs e)
        {
        }

        public void AddUrl(string url)
        {
            ViewCore.AddUrl(url);
        }

        public string GetUrl()
        {
            return ViewCore.GetUrl();
        }
    }
}
