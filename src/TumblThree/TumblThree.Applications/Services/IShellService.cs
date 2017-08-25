using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

using TumblThree.Applications.Properties;

namespace TumblThree.Applications.Services
{
    public interface IShellService : INotifyPropertyChanged
    {
        AppSettings Settings { get; }

        object ShellView { get; }

        object ContentView { get; set; }

        object DetailsView { get; set; }

        object QueueView { get; set; }

        object CrawlerView { get; set; }

        IReadOnlyCollection<Task> TasksToCompleteBeforeShutdown { get; }

        bool IsApplicationBusy { get; }

        ClipboardMonitor ClipboardMonitor { get; set; }

        OAuthManager OAuthManager { get; set; }

        event CancelEventHandler Closing;

        void ShowError(Exception exception, string displayMessage);

        void ShowDetailsView();

        void UpdateDetailsView();

        void ShowQueueView();

        void AddTaskToCompleteBeforeShutdown(Task task);

        IDisposable SetApplicationBusy();
    }
}
