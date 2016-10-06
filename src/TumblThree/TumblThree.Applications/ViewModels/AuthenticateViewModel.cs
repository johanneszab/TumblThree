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
    public class AuthenticateViewModel : ViewModel<IAuthenticateView>
    {

        [ImportingConstructor]
        public AuthenticateViewModel(IAuthenticateView view)
            : base(view)
        {
            view.Closed += ViewClosed;
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
