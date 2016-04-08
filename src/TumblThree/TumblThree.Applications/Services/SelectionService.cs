using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Waf.Foundation;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;
using System.ComponentModel;

namespace TumblThree.Applications.Services
{
    [Export, Export(typeof(ISelectionService))]
    internal class SelectionService : Model, ISelectionService
    {
        private readonly ObservableCollection<IBlog> selectedBlogFiles;
        private readonly ObservableCollection<IBlog> blogFiles;

        private readonly ObservableCollection<QueueListItem> activeItems;
        private readonly ReadOnlyObservableList<QueueListItem> readonlyActiveItems;
        [ImportingConstructor]
        public SelectionService()
        {
            this.selectedBlogFiles = new ObservableCollection<IBlog>();
            this.blogFiles = new ObservableCollection<IBlog>();

            this.activeItems = new ObservableCollection<QueueListItem>();
            this.readonlyActiveItems = new ReadOnlyObservableList<QueueListItem>(activeItems);

            this.activeItems.CollectionChanged += ActiveItemsCollectionChanged;

        }

        public ObservableCollection<IBlog> BlogFiles { get { return blogFiles; } }

        public IList<IBlog> SelectedBlogFiles { get { return selectedBlogFiles; } }

        public IReadOnlyObservableList<QueueListItem> ActiveItems { get { return readonlyActiveItems; } }

        public void AddActiveItems(QueueListItem itemToAdd)
        {
            activeItems.Add(itemToAdd);
        }

        public void RemoveActiveItem(QueueListItem itemToRemove)
        {
            activeItems.Remove(itemToRemove);
        }

        public void ClearItems()
        {
            activeItems.Clear();
        }

        private void ActiveItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                RaisePropertyChanged("ActiveItems");
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                RaisePropertyChanged("ActiveItems");
            }
        }
    }
}
