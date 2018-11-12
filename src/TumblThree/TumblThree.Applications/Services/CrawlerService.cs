using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Foundation;
using System.Windows.Input;

using Guava.RateLimiter;

using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Services
{
    [Export(typeof(ICrawlerService)), Export]
    public class CrawlerService : Model, ICrawlerService
    {
        private readonly ObservableCollection<QueueListItem> activeItems;
        private readonly ReadOnlyObservableList<QueueListItem> readonlyActiveItems;
        private ICommand addBlogCommand;
        private ICommand autoDownloadCommand;
        private ICommand crawlCommand;
        private ICommand enqueueSelectedCommand;
        private ICommand loadLibraryCommand;
        private ICommand loadAllDatabasesCommand;
        private ICommand checkIfDatabasesCompleteCommand;
        private bool isCrawl;
        private bool isPaused;
        private bool isTimerSet;
        private TaskCompletionSource<bool> libraryLoaded;
        private TaskCompletionSource<bool> databasesLoaded;
        private ICommand listenClipboardCommand;
        private string newBlogUrl;
        private ICommand pauseCommand;
        private ICommand removeBlogCommand;
        private ICommand removeBlogFromQueueCommand;
        private ICommand resumeCommand;
        private ICommand showFilesCommand;
        private ICommand stopCommand;
        private RateLimiter timeconstraint;
        private Timer timer;

        [ImportingConstructor]
        public CrawlerService(IShellService shellService)
        {
            timeconstraint =
                RateLimiter.Create(shellService.Settings.MaxConnections /
                                   (double)shellService.Settings.ConnectionTimeInterval);

            activeItems = new ObservableCollection<QueueListItem>();
            readonlyActiveItems = new ReadOnlyObservableList<QueueListItem>(activeItems);
            libraryLoaded = new TaskCompletionSource<bool>();
            databasesLoaded = new TaskCompletionSource<bool>();
            activeItems.CollectionChanged += ActiveItemsCollectionChanged;
        }

        public bool IsTimerSet
        {
            get => isTimerSet;
            set => SetProperty(ref isTimerSet, value);
        }

        public TaskCompletionSource<bool> LibraryLoaded
        {
            get => libraryLoaded;
            set => SetProperty(ref libraryLoaded, value);
        }

        public TaskCompletionSource<bool> DatabasesLoaded
        {
            get => databasesLoaded;
            set => SetProperty(ref databasesLoaded, value);
        }

        public Timer Timer
        {
            get => timer;
            set => SetProperty(ref timer, value);
        }

        public IReadOnlyObservableList<QueueListItem> ActiveItems => readonlyActiveItems;

        public ICommand AddBlogCommand
        {
            get => addBlogCommand;
            set => SetProperty(ref addBlogCommand, value);
        }

        public ICommand RemoveBlogCommand
        {
            get => removeBlogCommand;
            set => SetProperty(ref removeBlogCommand, value);
        }

        public ICommand ShowFilesCommand
        {
            get => showFilesCommand;
            set => SetProperty(ref showFilesCommand, value);
        }

        public ICommand EnqueueSelectedCommand
        {
            get => enqueueSelectedCommand;
            set => SetProperty(ref enqueueSelectedCommand, value);
        }

        public ICommand LoadLibraryCommand
        {
            get => loadLibraryCommand;
            set => SetProperty(ref loadLibraryCommand, value);
        }

        public ICommand LoadAllDatabasesCommand
        {
            get => loadAllDatabasesCommand;
            set => SetProperty(ref loadAllDatabasesCommand, value);
        }

        public ICommand CheckIfDatabasesCompleteCommand
        {
            get => checkIfDatabasesCompleteCommand;
            set => SetProperty(ref checkIfDatabasesCompleteCommand, value);
        }

        public ICommand RemoveBlogFromQueueCommand
        {
            get => removeBlogFromQueueCommand;
            set => SetProperty(ref removeBlogFromQueueCommand, value);
        }

        public ICommand ListenClipboardCommand
        {
            get => listenClipboardCommand;
            set => SetProperty(ref listenClipboardCommand, value);
        }

        public ICommand CrawlCommand
        {
            get => crawlCommand;
            set => SetProperty(ref crawlCommand, value);
        }

        public ICommand PauseCommand
        {
            get => pauseCommand;
            set => SetProperty(ref pauseCommand, value);
        }

        public ICommand ResumeCommand
        {
            get => resumeCommand;
            set => SetProperty(ref resumeCommand, value);
        }

        public ICommand StopCommand
        {
            get => stopCommand;
            set => SetProperty(ref stopCommand, value);
        }

        public ICommand AutoDownloadCommand
        {
            get => autoDownloadCommand;
            set => SetProperty(ref autoDownloadCommand, value);
        }

        public bool IsCrawl
        {
            get => isCrawl;
            set => SetProperty(ref isCrawl, value);
        }

        public bool IsPaused
        {
            get => isPaused;
            set => SetProperty(ref isPaused, value);
        }

        public string NewBlogUrl
        {
            get => newBlogUrl;
            set => SetProperty(ref newBlogUrl, value);
        }

        public RateLimiter Timeconstraint
        {
            get => timeconstraint;
            set => SetProperty(ref timeconstraint, value);
        }

        public void AddActiveItems(QueueListItem itemToAdd) => activeItems.Add(itemToAdd);

        public void RemoveActiveItem(QueueListItem itemToRemove) => activeItems.Remove(itemToRemove);

        public void ClearItems() => activeItems.Clear();

        private void ActiveItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add | e.Action == NotifyCollectionChangedAction.Remove)
                RaisePropertyChanged("ActiveItems");
        }
    }
}
