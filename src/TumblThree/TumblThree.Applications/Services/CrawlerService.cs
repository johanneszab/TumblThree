using System.ComponentModel.Composition;
using System.Windows.Input;
using System.Waf.Foundation;
using Guava.RateLimiter;

namespace TumblThree.Applications.Services
{
    [Export(typeof(ICrawlerService)), Export]
    public class CrawlerService : Model, ICrawlerService
    {
        private readonly IShellService shellService;
        private ICommand addBlogCommand;
        private ICommand removeBlogCommand;
        private ICommand showFilesCommand;
        private ICommand enqueueSelectedCommand;
        private ICommand removeBlogFromQueueCommand;
        private ICommand crawlCommand;
        private ICommand pauseCommand;
        private ICommand resumeCommand;
        private ICommand stopCommand;
        private ICommand listenClipboardCommand;
        private ICommand autoDownloadCommand;
        private bool isCrawl;
        private bool isPaused;
        private bool isTimerSet;
        private string newBlogUrl;
        private System.Threading.Timer timer;
        private RateLimiter timeconstraint;

        [ImportingConstructor]
        public CrawlerService(IShellService shellService)
        {
            this.shellService = shellService;
            timeconstraint = Guava.RateLimiter.RateLimiter.Create((double)shellService.Settings.MaxConnections / (double)shellService.Settings.ConnectionTimeInterval);
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

        public bool IsTimerSet
        {
            get { return isTimerSet; }
            set { SetProperty(ref isTimerSet, value); }
        }

        public System.Threading.Timer Timer
        {
            get { return timer; }
            set { SetProperty(ref timer, value); }
        }

        public string NewBlogUrl
        {
            get { return newBlogUrl; }
            set { SetProperty(ref newBlogUrl, value); }
        }

        public RateLimiter Timeconstraint
        {
            get { return timeconstraint; }
            set { SetProperty(ref timeconstraint, value); }
        }
    }
}
