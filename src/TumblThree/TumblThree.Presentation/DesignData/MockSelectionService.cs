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
        private ObservableCollection<IBlog> innerBlogFiles;
        private ObservableCollection<IBlog> selectedBlogFiles;
        private ObservableCollection<IBlog> blogFiles;

        private readonly ObservableCollection<QueueListItem> activeItems;
        private readonly ReadOnlyObservableList<QueueListItem> readonlyActiveItems;

        public MockSelectionService()
        {
            this.innerBlogFiles = new ObservableCollection<IBlog>();
            this.selectedBlogFiles = new ObservableCollection<IBlog>();
            this.blogFiles = new ObservableCollection<IBlog>(innerBlogFiles);

            this.activeItems = new ObservableCollection<QueueListItem>();
            this.readonlyActiveItems = new ReadOnlyObservableList<QueueListItem>(activeItems);
        }

        public ObservableCollection<IBlog> BlogFiles { get { return blogFiles; } }

        public IList<IBlog> SelectedBlogFiles { get { return selectedBlogFiles; } }

        public IReadOnlyObservableList<QueueListItem> ActiveItems { get { return readonlyActiveItems; } }

        public void SetBlogFiles(IEnumerable<IBlog> blogFilesToAdd)
        {
            innerBlogFiles.Clear();
            blogFilesToAdd.ToList().ForEach(x => innerBlogFiles.Add(x));
        }

        public void SetSelectedBlogFiles(IEnumerable<IBlog> blogFilesToAdd)
        {
            selectedBlogFiles.Clear();
            blogFilesToAdd.ToList().ForEach(x => this.selectedBlogFiles.Add(x));
        }

        public void SetActiveBlogFiles(IEnumerable<IBlog> blogFilesToAdd)
        {
            activeItems.Clear();
            blogFilesToAdd.ToList().ForEach(x => activeItems.Add(new QueueListItem(x)));
        }

    }
}
