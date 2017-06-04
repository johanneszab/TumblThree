using System.Waf.Applications;

namespace TumblThree.Applications.Views
{
    public interface IFullScreenMediaView : IView
    {
        void ShowDialog(object owner);
    }
}
