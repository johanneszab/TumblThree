using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Waf.Foundation;

namespace System.Waf.Applications
{
    /// <summary>
    /// Represents a collection that synchronizes all of it's items with the items of the specified original collection.
    /// When the original collection notifies a change via the <see cref="INotifyCollectionChanged"/> interface then
    /// this collection synchronizes it's own items with the original items.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <typeparam name="TOriginal">The type of elements in the original collection.</typeparam>
    public class SynchronizingCollection<T, TOriginal> : ReadOnlyObservableList<T>
    {
        private readonly ObservableCollection<T> innerCollection;
        private readonly List<Tuple<TOriginal, T>> mapping;
        private readonly IEnumerable<TOriginal> originalCollection;
        private readonly Func<TOriginal, T> factory;
        private readonly IEqualityComparer<T> itemComparer;
        private readonly IEqualityComparer<TOriginal> originalItemComparer;


        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizingCollection&lt;T, TOriginal&gt;"/> class.
        /// </summary>
        /// <param name="originalCollection">The original collection.</param>
        /// <param name="factory">The factory which is used to create new elements in this collection.</param>
        /// <exception cref="ArgumentNullException">The argument originalCollection must not be null.</exception>
        /// <exception cref="ArgumentNullException">The argument factory must not be null.</exception>
        public SynchronizingCollection(IEnumerable<TOriginal> originalCollection, Func<TOriginal, T> factory)
            : base(new ObservableCollection<T>())
        {
            if (originalCollection == null) { throw new ArgumentNullException("originalCollection"); }
            if (factory == null) { throw new ArgumentNullException("factory"); }

            mapping = new List<Tuple<TOriginal, T>>();
            this.originalCollection = originalCollection;
            this.factory = factory;
            itemComparer = EqualityComparer<T>.Default;
            originalItemComparer = EqualityComparer<TOriginal>.Default;

            INotifyCollectionChanged collectionChanged = originalCollection as INotifyCollectionChanged;
            if (collectionChanged != null)
            {
                CollectionChangedEventManager.AddHandler(collectionChanged, OriginalCollectionChanged);
            }

            innerCollection = (ObservableCollection<T>)Items;
            foreach (TOriginal item in originalCollection)
            {
                innerCollection.Add(CreateItem(item));
            }
        }


        private T CreateItem(TOriginal oldItem)
        {
            T newItem = factory(oldItem);
            mapping.Add(new Tuple<TOriginal, T>(oldItem, newItem));
            return newItem;
        }

        private bool RemoveCore(TOriginal oldItem)
        {
            Tuple<TOriginal, T> tuple = mapping.First(t => originalItemComparer.Equals(t.Item1, oldItem));
            mapping.Remove(tuple);
            return innerCollection.Remove(tuple.Item2);
        }

        private void RemoveAtCore(int index)
        {
            T newItem = this[index];
            Tuple<TOriginal, T> tuple = mapping.First(t => itemComparer.Equals(t.Item2, newItem));
            mapping.Remove(tuple);
            innerCollection.RemoveAt(index);
        }

        private void ClearCore()
        {
            innerCollection.Clear();
            mapping.Clear();
        }

        private void OriginalCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewStartingIndex >= 0)
                {
                    int i = e.NewStartingIndex;
                    foreach (TOriginal item in e.NewItems)
                    {
                        innerCollection.Insert(i, CreateItem(item));
                        i++;
                    }
                }
                else
                {
                    foreach (TOriginal item in e.NewItems)
                    {
                        innerCollection.Add(CreateItem(item));
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldStartingIndex >= 0)
                {
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        RemoveAtCore(e.OldStartingIndex);
                    }
                }
                else
                {
                    foreach (TOriginal item in e.OldItems)
                    {
                        RemoveCore(item);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (e.NewStartingIndex >= 0)
                {
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        innerCollection[i + e.NewStartingIndex] = CreateItem((TOriginal)e.NewItems[i]);
                    }
                }
                else
                {
                    foreach (TOriginal item in e.OldItems) { RemoveCore(item); }
                    foreach (TOriginal item in e.NewItems) { innerCollection.Add(CreateItem(item)); }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Move)
            {
                for (int i = 0; i < e.NewItems.Count; i++)
                {
                    innerCollection.Move(e.OldStartingIndex + i, e.NewStartingIndex + i);
                }
            }
            else // Reset
            {
                ClearCore();
                foreach (TOriginal item in originalCollection)
                {
                    innerCollection.Add(CreateItem(item));
                }
            }
        }
    }
}
