using System;
using System.Collections.Generic;
using System.Waf.Applications;
using TumblThree.Applications.Views;
using TumblThree.Domain;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
