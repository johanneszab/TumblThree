using System.ComponentModel.Composition;
using System.Waf.Applications;

using TumblThree.Applications.Views;

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
