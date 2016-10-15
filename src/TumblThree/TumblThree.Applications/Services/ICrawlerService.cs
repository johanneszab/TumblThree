using System.ComponentModel;
using System.Windows.Input;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Services
{
    public interface ICrawlerService : INotifyPropertyChanged
    {
        ICommand AddBlogCommand { get; }

        ICommand RemoveBlogCommand { get; }

        ICommand ShowFilesCommand { get; }

        ICommand EnqueueSelectedCommand { get; }

        ICommand RemoveBlogFromQueueCommand { get; }

        ICommand ListenClipboardCommand { get; }

        ICommand CrawlCommand { get; }

        ICommand PauseCommand { get; }

        ICommand ResumeCommand { get; }

        ICommand StopCommand { get; }

        ICommand AutoDownloadCommand { get; }

        bool IsCrawl { get; }

        bool IsPaused { get; }

        string NewBlogUrl { get; set; }

    }
}
