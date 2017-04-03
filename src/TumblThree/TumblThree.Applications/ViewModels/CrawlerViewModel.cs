using System.ComponentModel.Composition;
using System.Waf.Applications;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;

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
