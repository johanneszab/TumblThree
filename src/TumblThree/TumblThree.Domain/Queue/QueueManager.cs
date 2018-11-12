using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Waf.Foundation;

namespace TumblThree.Domain.Queue
{
    public class QueueManager : Model
    {
        private readonly ObservableCollection<QueueListItem> items;
        private readonly ReadOnlyObservableList<QueueListItem> readonlyItems;
        private int queueDownloadedImageCount;

        private int queueTotalImageCount;

        public QueueManager()
        {
            items = new ObservableCollection<QueueListItem>();
            readonlyItems = new ReadOnlyObservableList<QueueListItem>(items);

            items.CollectionChanged += ItemsCollectionChanged;
        }

        public IReadOnlyObservableList<QueueListItem> Items => readonlyItems;

        public int QueueTotalImageCount
        {
            get => queueTotalImageCount;
            private set => SetProperty(ref queueTotalImageCount, value);
        }

        public int QueueDownloadedImageCount
        {
            get => queueDownloadedImageCount;
            private set => SetProperty(ref queueDownloadedImageCount, value);
        }

        public void AddAndReplaceItems(IEnumerable<QueueListItem> itemsToAdd)
        {
            items.Clear();
            AddItems(itemsToAdd);
        }

        public void AddItems(IEnumerable<QueueListItem> itemsToAdd) => InsertItems(items.Count, itemsToAdd);

        public void InsertItems(int index, IEnumerable<QueueListItem> itemsToInsert)
        {
            foreach (QueueListItem item in itemsToInsert)
                items.Insert(index++, item);
        }

        public void RemoveItems(IEnumerable<QueueListItem> itemsToRemove)
        {
            foreach (QueueListItem item in itemsToRemove.ToArray())
                items.Remove(item);
        }

        public void RemoveItem(QueueListItem itemToRemove) => items.Remove(itemToRemove);

        public void ClearItems() => items.Clear();

        public void MoveItems(int newIndex, IEnumerable<QueueListItem> itemsToMove)
        {
            List<QueueListItem> listItems = itemsToMove.ToList();

            int oldIndex = items.IndexOf(listItems.First());
            if (oldIndex != newIndex)
            {
                if (newIndex < oldIndex)
                {
                    listItems.Reverse();
                }

                foreach (QueueListItem item in listItems)
                {
                    int currentIndex = items.IndexOf(item);
                    if (currentIndex != newIndex)
                    {
                        items.Move(currentIndex, newIndex);
                    }
                }
            }
        }

        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add | e.Action == NotifyCollectionChangedAction.Remove)
                UpdateTotalImageCount();
        }

        private void UpdateDownloadedImageCount()
        {
            if (items.Any())
            {
                IEnumerable<QueueListItem> loadedItems = items.Where(x => x.Blog.DownloadedImages > 0);

                QueueDownloadedImageCount =
                    loadedItems.Select(x => x.Blog.DownloadedImages).Aggregate((current, next) => current + next);
            }
            else
            {
                QueueDownloadedImageCount = 0;
            }
        }

        private void UpdateTotalImageCount()
        {
            if (items.Any())
            {
                IEnumerable<QueueListItem> loadedItems = items.Where(x => x.Blog.TotalCount > 0);

                QueueTotalImageCount =
                    loadedItems.Select(x => x.Blog.TotalCount).DefaultIfEmpty().Aggregate((current, next) => current + next);
            }
            else
            {
                QueueTotalImageCount = 0;
            }
        }
    }
}
