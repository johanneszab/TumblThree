using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace System.Waf.Foundation
{
    /// <summary>
    /// Represents a read-only <see cref="System.Collections.ObjectModel.ObservableCollection&lt;T&gt;"/>.
    /// This class implements the IReadOnlyObservableList interface and provides public CollectionChanged and PropertyChanged events.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    [Serializable]
    public class ReadOnlyObservableList<T> : ReadOnlyObservableCollection<T>, IReadOnlyObservableList<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="System.Collections.ObjectModel.ReadOnlyObservableCollection&lt;T&gt;"/>
        /// class that serves as a wrapper around the specified <see cref="System.Collections.ObjectModel.ObservableCollection&lt;T&gt;"/>.
        /// </summary>
        /// <param name="list">
        /// The <see cref="System.Collections.ObjectModel.ObservableCollection&lt;T&gt;"/> with which to
        /// create this instance of the <see cref="System.Collections.ObjectModel.ReadOnlyObservableCollection&lt;T&gt;"/>
        /// class.</param>
        /// <exception cref="ArgumentNullException">list is null.</exception>
        public ReadOnlyObservableList(ObservableCollection<T> list)
            : base(list)
        {
        }


        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public new event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add { base.CollectionChanged += value; }
            remove { base.CollectionChanged -= value; }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public new event PropertyChangedEventHandler PropertyChanged
        {
            add { base.PropertyChanged += value; }
            remove { base.PropertyChanged -= value; }
        }
    }
}
