using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Waf.Foundation;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Presentation.DesignData
{
    public class MockSelectionService : ISelectionService
    {
        private ObservableCollection<IBlog> selectedBlogFiles;

        public MockSelectionService()
        {
            this.selectedBlogFiles = new ObservableCollection<IBlog>();
        }

        public IList<IBlog> SelectedBlogFiles { get { return selectedBlogFiles; } }

        public void SetSelectedBlogFiles(IEnumerable<IBlog> blogFilesToAdd)
        {
            selectedBlogFiles.Clear();
            blogFilesToAdd.ToList().ForEach(x => this.selectedBlogFiles.Add(x));
        }
    }
}
