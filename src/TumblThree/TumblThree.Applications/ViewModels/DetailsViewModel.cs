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
    public class DetailsViewModel : ViewModel<IDetailsView>
    {
        private readonly IClipboardService clipboardService;
        private readonly DelegateCommand copyUrlCommand;
        private Blog blogFile;
        private int count;

        [ImportingConstructor]
        public DetailsViewModel(IDetailsView view, IClipboardService clipboardService) : base(view)
        {
            this.clipboardService = clipboardService;
            this.copyUrlCommand = new DelegateCommand(CopyUrlToClipboard);
        }

        public ICommand CopyUrlCommand { get { return copyUrlCommand; } }

        public Blog BlogFile
        {
            get { return blogFile; }
            set { SetProperty(ref blogFile, value); }
        }

        public int Count
        {
            get { return count; }
            set { SetProperty(ref count, value); }
        }

        private void CopyUrlToClipboard()
        {
            if (BlogFile != null)
            {
                clipboardService.SetText(BlogFile.Url);
            }
        }
    }
}
