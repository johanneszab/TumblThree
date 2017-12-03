using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows.Input;

using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.ViewModels
{
    [Export(typeof(IDetailsViewModel))]
    [ExportMetadata("BlogType", typeof(TumblrSearchBlog))]
    public class DetailsTumblrSearchViewModel : ViewModel<IDetailsView>, IDetailsViewModel
    {
        private readonly IClipboardService clipboardService;
        private readonly DelegateCommand copyUrlCommand;
        private IBlog blogFile;
        private int count = 0;

        [ImportingConstructor]
        public DetailsTumblrSearchViewModel([Import("TumblrSearchView", typeof(IDetailsView))]IDetailsView view, IClipboardService clipboardService) : base(view)
        {
            this.clipboardService = clipboardService;
            copyUrlCommand = new DelegateCommand(CopyUrlToClipboard);
        }

        public ICommand CopyUrlCommand
        {
            get { return copyUrlCommand; }
        }

        public IBlog BlogFile
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
