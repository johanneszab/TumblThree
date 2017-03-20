using Guava.RateLimiter;
using System.ComponentModel;
using System.Windows.Input;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Services
{
    public interface ICrawlerService : INotifyPropertyChanged
    {
        ICommand AddBlogCommand { get; set; }

        ICommand RemoveBlogCommand { get; set; }

        ICommand ShowFilesCommand { get; set; }

        ICommand EnqueueSelectedCommand { get; set; }

        ICommand RemoveBlogFromQueueCommand { get; set; }

        ICommand ListenClipboardCommand { get; set; }

        ICommand CrawlCommand { get; set; }

        ICommand PauseCommand { get; set; }

        ICommand ResumeCommand { get; set; }

        ICommand StopCommand { get; set; }

        ICommand AutoDownloadCommand { get; set; }

        bool IsCrawl { get; set; }

        bool IsPaused { get; set; }

        string NewBlogUrl { get; set; }

        RateLimiter Timeconstraint { get; set; }

    }
}
