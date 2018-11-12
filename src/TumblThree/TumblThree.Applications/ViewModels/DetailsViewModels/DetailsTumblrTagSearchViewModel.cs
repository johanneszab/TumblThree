using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows.Forms;
using System.Windows.Input;

using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.ViewModels.DetailsViewModels
{
    [Export(typeof(IDetailsViewModel))]
    [ExportMetadata("BlogType", typeof(TumblrTagSearchBlog))]
    public class DetailsTumblrTagSearchViewModel : ViewModel<IDetailsView>, IDetailsViewModel
    {
        private readonly DelegateCommand browseFileDownloadLocationCommand;
        private readonly DelegateCommand copyUrlCommand;

        private readonly IClipboardService clipboardService;
        private IBlog blogFile;
        private int count = 0;

        [ImportingConstructor]
        public DetailsTumblrTagSearchViewModel([Import("TumblrTagSearchView", typeof(IDetailsView))]
            IDetailsView view,
            IClipboardService clipboardService) : base(view)
        {
            this.clipboardService = clipboardService;
            copyUrlCommand = new DelegateCommand(CopyUrlToClipboard);
            browseFileDownloadLocationCommand = new DelegateCommand(BrowseFileDownloadLocation);
        }

        public ICommand CopyUrlCommand => copyUrlCommand;

        public ICommand BrowseFileDownloadLocationCommand => browseFileDownloadLocationCommand;

        public IBlog BlogFile
        {
            get => blogFile;
            set => SetProperty(ref blogFile, value);
        }

        public int Count
        {
            get => count;
            set => SetProperty(ref count, value);
        }

        private void CopyUrlToClipboard()
        {
            if (BlogFile != null)
                clipboardService.SetText(BlogFile.Url);
        }

        private void BrowseFileDownloadLocation()
        {
            var dialog = new FolderBrowserDialog { SelectedPath = BlogFile.FileDownloadLocation };
            if (dialog.ShowDialog() == DialogResult.OK)
                BlogFile.FileDownloadLocation = dialog.SelectedPath;
        }
    }
}
