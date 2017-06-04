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
using TumblThree.Domain.Models;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class FullScreenMediaViewModel : ViewModel<IFullScreenMediaView>
    {
        [ImportingConstructor]
        public FullScreenMediaViewModel(IFullScreenMediaView view)
            : base(view)
        {
        }

        public void ShowDialog(object owner)
        {
            ViewCore.ShowDialog(owner);
        }
    }
}
