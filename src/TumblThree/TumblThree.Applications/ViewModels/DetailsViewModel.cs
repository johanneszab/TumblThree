using System.Waf.Applications;
using TumblThree.Applications.Views;
using System.ComponentModel.Composition;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class DetailsViewModel : ViewModel<IDetailsView>
    {
        [ImportingConstructor]
        public DetailsViewModel(IDetailsView view)
            : base(view)
        {
        }
    }
}
