using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Waf.Foundation;
using System.Windows.Input;

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
        private bool isCrawl;
        private bool isPaused;
        private bool isTimerSet;
        private TaskCompletionSource<bool> databasesLoaded;
        private ICommand listenClipboardCommand;
        private string newBlogUrl;
        private ICommand pauseCommand;
        private ICommand removeBlogCommand;
        private ICommand removeBlogFromQueueCommand;
        private ICommand resumeCommand;
        private ICommand showFilesCommand;
        private ICommand stopCommand;
        private System.Threading.Timer timer;

        [ImportingConstructor]
        public CrawlerService(IShellService shellService)
        {
            activeItems = new ObservableCollection<QueueListItem>();
            readonlyActiveItems = new ReadOnlyObservableList<QueueListItem>(activeItems);
            databasesLoaded = new TaskCompletionSource<bool>();
            activeItems.CollectionChanged += ActiveItemsCollectionChanged;
        }

        public bool IsTimerSet
        {
            get { return isTimerSet; }
            set { SetProperty(ref isTimerSet, value); }
        }

        public TaskCompletionSource<bool> DatabasesLoaded
        {
            get { return databasesLoaded; }
            set { SetProperty(ref databasesLoaded, value); }
        }

        public System.Threading.Timer Timer
        {
            get { return timer; }
            set { SetProperty(ref timer, value); }
        }

        public IReadOnlyObservableList<QueueListItem> ActiveItems
        {
            get { return readonlyActiveItems; }
        }

        public ICommand AddBlogCommand
        {
            get { return addBlogCommand; }
            set { SetProperty(ref addBlogCommand, value); }
        }

        public ICommand RemoveBlogCommand
        {
            get { return removeBlogCommand; }
            set { SetProperty(ref removeBlogCommand, value); }
        }

        public ICommand ShowFilesCommand
        {
            get { return showFilesCommand; }
            set { SetProperty(ref showFilesCommand, value); }
        }

        public ICommand EnqueueSelectedCommand
        {
            get { return enqueueSelectedCommand; }
            set { SetProperty(ref enqueueSelectedCommand, value); }
        }

        public ICommand LoadLibraryCommand
        {
            get { return loadLibraryCommand; }
            set { SetProperty(ref loadLibraryCommand, value); }
        }

        public ICommand LoadAllDatabasesCommand
        {
            get { return loadAllDatabasesCommand; }
            set { SetProperty(ref loadAllDatabasesCommand, value); }
        }

        public ICommand RemoveBlogFromQueueCommand
        {
            get { return removeBlogFromQueueCommand; }
            set { SetProperty(ref removeBlogFromQueueCommand, value); }
        }

        public ICommand ListenClipboardCommand
        {
            get { return listenClipboardCommand; }
            set { SetProperty(ref listenClipboardCommand, value); }
        }

        public ICommand CrawlCommand
        {
            get { return crawlCommand; }
            set { SetProperty(ref crawlCommand, value); }
        }

        public ICommand PauseCommand
        {
            get { return pauseCommand; }
            set { SetProperty(ref pauseCommand, value); }
        }

        public ICommand ResumeCommand
        {
            get { return resumeCommand; }
            set { SetProperty(ref resumeCommand, value); }
        }

        public ICommand StopCommand
        {
            get { return stopCommand; }
            set { SetProperty(ref stopCommand, value); }
        }

        public ICommand AutoDownloadCommand
        {
            get { return autoDownloadCommand; }
            set { SetProperty(ref autoDownloadCommand, value); }
        }

        public bool IsCrawl
        {
            get { return isCrawl; }
            set { SetProperty(ref isCrawl, value); }
        }

        public bool IsPaused
        {
            get { return isPaused; }
            set { SetProperty(ref isPaused, value); }
        }

        public string NewBlogUrl
        {
            get { return newBlogUrl; }
            set { SetProperty(ref newBlogUrl, value); }
        }

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
