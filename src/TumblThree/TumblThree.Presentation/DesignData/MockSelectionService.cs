using System.Collections.Generic;
using System.Linq;

using TumblThree.Applications.ObjectModel;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models;

namespace TumblThree.Presentation.DesignData
{
    public class MockSelectionService : ISelectionService
    {
        private readonly ObservableRangeCollection<IBlog> selectedBlogFiles;

        public MockSelectionService()
        {
            selectedBlogFiles = new ObservableRangeCollection<IBlog>();
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

        public void AddRange(IEnumerable<IBlog> collection)
        {
            selectedBlogFiles.AddRange(collection);
        }

        public void RemoveRange(IEnumerable<IBlog> collection)
        {
            selectedBlogFiles.RemoveRange(collection);
        }
    }
}
