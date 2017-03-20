using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Waf.Applications;
using System.Windows.Input;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class CrawlerViewModel : ViewModel<ICrawlerView>
    {
        [ImportingConstructor]
        public CrawlerViewModel(ICrawlerView view, IShellService shellService, ICrawlerService crawlerService) : base(view)
        {
            CrawlerService = crawlerService;
            ShellService = shellService;
        }

        public IShellService ShellService { get; }

        public ICrawlerService CrawlerService { get; }

    }
}
