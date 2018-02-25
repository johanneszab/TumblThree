using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows.Input;

using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.ViewModels
{
    [Export(typeof(IDetailsViewModel))]
    [ExportMetadata("BlogType", typeof(TumblrTagSearchBlog))]
    public class DetailsTumblrTagSearchViewModel : ViewModel<IDetailsView>, IDetailsViewModel
    {
        private readonly IClipboardService clipboardService;
        private readonly DelegateCommand copyUrlCommand;
        private readonly DelegateCommand browseFileDownloadLocationCommand;
        private IBlog blogFile;
        private int count = 0;

        [ImportingConstructor]
        public DetailsTumblrTagSearchViewModel([Import("TumblrTagSearchView", typeof(IDetailsView))]IDetailsView view, IClipboardService clipboardService) : base(view)
        {
            this.clipboardService = clipboardService;
            copyUrlCommand = new DelegateCommand(CopyUrlToClipboard);
            browseFileDownloadLocationCommand = new DelegateCommand(BrowseFileDownloadLocation);
        }

        public ICommand CopyUrlCommand
        {
            get { return copyUrlCommand; }
        }

        public ICommand BrowseFileDownloadLocationCommand
        {
            get { return browseFileDownloadLocationCommand; }
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

        private void BrowseFileDownloadLocation()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = BlogFile.FileDownloadLocation };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                BlogFile.FileDownloadLocation = dialog.SelectedPath;
            }
        }
    }
}
