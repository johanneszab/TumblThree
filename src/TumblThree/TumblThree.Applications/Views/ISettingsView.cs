using System;
using System.Waf.Applications;

namespace TumblThree.Applications.Views
{
    public interface ISettingsView : IView
    {
        void ShowDialog(object owner);

        event EventHandler Closed;
    }
}
