using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Waf.Foundation;
using TumblThree.Domain.Models;

namespace TumblThree.Domain.Queue
{
    public class QueueManager : Model
    {
        private readonly ObservableCollection<QueueListItem> items;
        private readonly ReadOnlyObservableList<QueueListItem> readonlyItems;

        private uint queueTotalImageCount;
        private uint queueDownloadedImageCount;

        public QueueManager()
        {
            this.items = new ObservableCollection<QueueListItem>();
            this.readonlyItems = new ReadOnlyObservableList<QueueListItem>(items);

            this.items.CollectionChanged += ItemsCollectionChanged;
        }

        public IReadOnlyObservableList<QueueListItem> Items { get { return readonlyItems; } }

        public void AddAndReplaceItems(IEnumerable<QueueListItem> itemsToAdd)
        {
            items.Clear();
            AddItems(itemsToAdd);
        }

        public void AddItems(IEnumerable<QueueListItem> itemsToAdd)
        {
            InsertItems(items.Count, itemsToAdd);
        }

        public void InsertItems(int index, IEnumerable<QueueListItem> itemsToInsert)
        {
            foreach (var item in itemsToInsert)
            {
                items.Insert(index++, item);
            }
        }

        public void RemoveItems(IEnumerable<QueueListItem> itemsToRemove)
        {
            foreach (var item in itemsToRemove.ToArray())
            {
                items.Remove(item);
            }
        }

        public void RemoveItem(QueueListItem itemToRemove)
        {
            items.Remove(itemToRemove);
        }

        public void ClearItems()
        {
            items.Clear();
        }

        public void MoveItems(int newIndex, IEnumerable<QueueListItem> itemsToMove)
        {
            int oldIndex = items.IndexOf(itemsToMove.First());
            if (oldIndex != newIndex)
            {
                if (newIndex < oldIndex)
                {
                    itemsToMove = itemsToMove.Reverse();
                }

                foreach (var item in itemsToMove)
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
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                UpdateTotalImageCount();
                
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                UpdateTotalImageCount();
            }
        }

        public uint QueueTotalImageCount
        {
            get { return queueTotalImageCount; }
            private set { SetProperty(ref queueTotalImageCount, value); }
        }

        public uint QueueDownloadedImageCount
        {
            get { return queueDownloadedImageCount; }
            private set { SetProperty(ref queueDownloadedImageCount, value); }
        }

        private void UpdateDownloadedImageCount()
        {
            if (items.Any())
            {
                var loadedItems = items.Where(x => x.Blog.DownloadedImages > 0);

                QueueDownloadedImageCount = loadedItems.Select(x => x.Blog.DownloadedImages).Aggregate((current, next) => current + next);
            } else
            {
                QueueDownloadedImageCount = 0;
            }
        }

        private void UpdateTotalImageCount()
        {
            if (items.Any())
            {
                var loadedItems = items.Where(x => x.Blog.TotalCount > 0);

                QueueTotalImageCount = loadedItems.Select(x => x.Blog.TotalCount).DefaultIfEmpty().Aggregate((current, next) => current + next);
            } else
            {
                QueueTotalImageCount = 0;
            }
        }
    }
}
