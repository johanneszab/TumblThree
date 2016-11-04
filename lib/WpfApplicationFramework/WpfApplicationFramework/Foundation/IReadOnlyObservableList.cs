using System.Collections.Generic;
using System.Collections.Specialized;

namespace System.Waf.Foundation
{
    /// <summary>
    /// Represents a read-only list of elements that can be accessed by index. Additional the list
    /// notifies listeners of changes, such as when items get added and removed.
    /// </summary>
    /// <typeparam name="T">
    /// The type of elements in the read-only list. This type parameter is covariant.
    /// That is, you can use either the type you specified or any type that is more
    /// derived.
    /// </typeparam>
    public interface IReadOnlyObservableList<out T> : IReadOnlyList<T>, INotifyCollectionChanged
    {
    }
}
