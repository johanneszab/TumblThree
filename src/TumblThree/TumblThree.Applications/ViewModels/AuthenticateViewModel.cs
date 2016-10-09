using System;
using System.Waf.Applications;
using TumblThree.Applications.Views;
using System.ComponentModel.Composition;
using TumblThree.Applications.Services;

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
            this.oauthCallbackUrl = shellService.Settings.OAuthCallbackUrl;
        }

        public IShellService ShellService { get; }

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

        public string OAuthCallbackUrl
        {
            get { return oauthCallbackUrl; }
            set { SetProperty(ref oauthCallbackUrl, value); }
        }
    }
}
