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

        IReadOnlyCollection<Task> TasksToCompleteBeforeShutdown { get; }

        bool IsApplicationBusy { get; }

        event CancelEventHandler Closing;

        ClipboardMonitor ClipboardMonitor { get; set; }

        void ShowError(Exception exception, string displayMessage);

        void ShowDetailsView();

        void ShowQueueView();

        void AddTaskToCompleteBeforeShutdown(Task task);

        IDisposable SetApplicationBusy();

        OAuthManager OAuthManager { get; set; }
    }
}
