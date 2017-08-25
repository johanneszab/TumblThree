using System.Waf.Applications;
using System.Windows.Input;
using System.Windows.Media;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Views
{
    public interface IDetailsViewModel
    {
        IBlog BlogFile { get; set; }

        int Count { get; set; }

        object View { get; }
    }
}
