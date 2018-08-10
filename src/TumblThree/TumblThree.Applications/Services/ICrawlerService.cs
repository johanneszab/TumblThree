using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Foundation;
using System.Windows.Input;

using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Services
{
    public interface ICrawlerService : INotifyPropertyChanged
    {
        ICommand AddBlogCommand { get; set; }

        ICommand RemoveBlogCommand { get; set; }

        ICommand ShowFilesCommand { get; set; }

        ICommand EnqueueSelectedCommand { get; set; }

        ICommand LoadLibraryCommand { get; set; }

        ICommand LoadAllDatabasesCommand { get; set; }

        ICommand RemoveBlogFromQueueCommand { get; set; }

        ICommand ListenClipboardCommand { get; set; }

        ICommand CrawlCommand { get; set; }

        ICommand PauseCommand { get; set; }

        ICommand ResumeCommand { get; set; }

        ICommand StopCommand { get; set; }

        ICommand AutoDownloadCommand { get; set; }

        bool IsCrawl { get; set; }

        bool IsPaused { get; set; }

        bool IsTimerSet { get; set; }

        string NewBlogUrl { get; set; }

        IReadOnlyObservableList<QueueListItem> ActiveItems { get; }

        Timer Timer { get; set; }

        TaskCompletionSource<bool> DatabasesLoaded { get; set; }

        void AddActiveItems(QueueListItem itemToAdd);

        void RemoveActiveItem(QueueListItem itemToRemove);
    }
}
