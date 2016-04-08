using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Waf.Foundation;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Views;

namespace TumblThree.Applications.Services
{
    [Export(typeof(IShellService)), Export]
    internal class ShellService : Model, IShellService
    {
        private readonly Lazy<IShellView> shellView;
        private readonly List<Task> tasksToCompleteBeforeShutdown;
        private readonly List<ApplicationBusyContext> applicationBusyContext;
        private object contentView;
        private object detailsView;
        private object queueView;
        private object settingsView;
        private object aboutView;
        private bool isApplicationBusy;
        private bool isClosingEventInitialized;
        private event CancelEventHandler closing;
        private ClipboardMonitor clipboardMonitor;

        [ImportingConstructor]
        public ShellService(Lazy<IShellView> shellView)
        {
            this.shellView = shellView;
            this.tasksToCompleteBeforeShutdown = new List<Task>();
            this.applicationBusyContext = new List<ApplicationBusyContext>();
            this.clipboardMonitor = new ClipboardMonitor();
        }

        public AppSettings Settings { get; set; }

        public object ShellView { get { return shellView.Value; } }

        public object ContentView
        {
            get { return contentView; }
            set { SetProperty(ref contentView, value); }
        }

        public object DetailsView
        {
            get { return detailsView; }
            set { SetProperty(ref detailsView, value); }
        }

        public object QueueView
        {
            get { return queueView; }
            set { SetProperty(ref queueView, value); }
        }

        public object SettingsView
        {
            get { return settingsView; }
            set { SetProperty(ref settingsView, value); }
        }

        public object AboutView
        {
            get { return aboutView; }
            set { SetProperty(ref aboutView, value); }
        }

        public Action<Exception, string> ShowErrorAction { get; set; }
        
        public Action ShowDetailsViewAction { get; set; }

        public Action ShowQueueViewAction { get; set; }

        public IReadOnlyCollection<Task> TasksToCompleteBeforeShutdown { get { return tasksToCompleteBeforeShutdown; } }

        public bool IsApplicationBusy
        {
            get { return isApplicationBusy; }
            private set { SetProperty(ref isApplicationBusy, value); }
        }


        public event CancelEventHandler Closing
        {
            add
            {
                closing += value;
                InitializeClosingEvent();
            }
            remove { closing -= value; }
        }


        public void ShowError(Exception exception, string displayMessage)
        {
            ShowErrorAction(exception, displayMessage);
        }

        public void ShowDetailsView()
        {
            ShowDetailsViewAction();
        }

        public void ShowQueueView()
        {
            ShowQueueViewAction();
        }

        public void AddTaskToCompleteBeforeShutdown(Task task)
        {
            tasksToCompleteBeforeShutdown.Add(task);
        }

        public IDisposable SetApplicationBusy()
        {
            var context = new ApplicationBusyContext()
            {
                DisposeCallback = ApplicationBusyContextDisposeCallback
            };
            applicationBusyContext.Add(context);
            IsApplicationBusy = true;
            return context;
        }

        protected virtual void OnClosing(CancelEventArgs e)
        {
            closing?.Invoke(this, e);
        }

        private void ApplicationBusyContextDisposeCallback(ApplicationBusyContext context)
        {
            applicationBusyContext.Remove(context);
            IsApplicationBusy = applicationBusyContext.Any();
        }

        private void InitializeClosingEvent()
        {
            if (isClosingEventInitialized) { return; }

            isClosingEventInitialized = true;
            shellView.Value.Closing += ShellViewClosing;
        }

        private void ShellViewClosing(object sender, CancelEventArgs e)
        {
            OnClosing(e);
        }

        public ClipboardMonitor ClipboardMonitor
        {
            get { return clipboardMonitor; }
            set { SetProperty(ref clipboardMonitor, value); }
        }
    }
}
