using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using TumblThree.Applications.Services;
using TumblThree.Domain.Models;

namespace TumblThree.Presentation.DesignData
{
    public class MockSelectionService : ISelectionService
    {
        private readonly ObservableCollection<IBlog> selectedBlogFiles;

        public MockSelectionService()
        {
            selectedBlogFiles = new ObservableCollection<IBlog>();
        }

        public IList<IBlog> SelectedBlogFiles
        {
            get { return selectedBlogFiles; }
        }

        public void SetSelectedBlogFiles(IEnumerable<IBlog> blogFilesToAdd)
        {
            selectedBlogFiles.Clear();
            blogFilesToAdd.ToList().ForEach(x => selectedBlogFiles.Add(x));
        }
    }
}
