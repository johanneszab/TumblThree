using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Waf.Foundation;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Services
{
    public interface ISelectionService
    {
        ObservableCollection<IBlog> BlogFiles { get; }

        IList<IBlog> SelectedBlogFiles { get; }

        IReadOnlyObservableList<QueueListItem> ActiveItems { get; }

        void AddActiveItems(QueueListItem itemToAdd);

        void RemoveActiveItem(QueueListItem itemToRemove);

    }
}