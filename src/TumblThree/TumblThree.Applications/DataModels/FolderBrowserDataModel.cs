using System.Collections.Generic;
using System.Waf.Foundation;

namespace TumblThree.Applications.DataModels
{
    public class FolderBrowserDataModel : Model
    {
        private string blogPath;
        private string indexPath;
        private FolderItem selectedSubDirectory;
        private IReadOnlyList<FolderItem> subDirectories;

        public string IndexPath
        {
            get { return indexPath; }
            set { SetProperty(ref indexPath, value ?? ""); }
        }

        public string BlogPath
        {
            get { return blogPath; }
            set { SetProperty(ref blogPath, value ?? ""); }
        }

        public IReadOnlyList<FolderItem> SubDirectories
        {
            get { return subDirectories; }
            set { SetProperty(ref subDirectories, value); }
        }

        public FolderItem SelectedSubDirectory
        {
            get { return selectedSubDirectory; }
            set { SetProperty(ref selectedSubDirectory, value); }
        }
    }
}
