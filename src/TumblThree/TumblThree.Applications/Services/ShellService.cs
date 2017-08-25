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
        private readonly List<ApplicationBusyContext> applicationBusyContext;
        private readonly Lazy<IShellView> shellView;
        private readonly List<Task> tasksToCompleteBeforeShutdown;
        private object aboutView;
        private ClipboardMonitor clipboardMonitor;
        private object contentView;
        private object crawlerView;
        private object detailsView;
        private bool isApplicationBusy;
        private bool isClosingEventInitialized;
        private OAuthManager oauthManager;
        private object queueView;
        private object settingsView;

        [ImportingConstructor]
        public ShellService(Lazy<IShellView> shellView)
        {
            this.shellView = shellView;
            tasksToCompleteBeforeShutdown = new List<Task>();
            applicationBusyContext = new List<ApplicationBusyContext>();
            clipboardMonitor = new ClipboardMonitor();
            oauthManager = new OAuthManager();
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

        public Action UpdateDetailsViewAction { get; set; }

        public AppSettings Settings { get; set; }

        public object ShellView
        {
            get { return shellView.Value; }
        }

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

        public object CrawlerView
        {
            get { return crawlerView; }
            set { SetProperty(ref crawlerView, value); }
        }

        public IReadOnlyCollection<Task> TasksToCompleteBeforeShutdown
        {
            get { return tasksToCompleteBeforeShutdown; }
        }

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

        public void UpdateDetailsView()
        {
            UpdateDetailsViewAction();
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

        public ClipboardMonitor ClipboardMonitor
        {
            get { return clipboardMonitor; }
            set { SetProperty(ref clipboardMonitor, value); }
        }

        public OAuthManager OAuthManager
        {
            get { return oauthManager; }
            set { SetProperty(ref oauthManager, value); }
        }

        private event CancelEventHandler closing;

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
            if (isClosingEventInitialized)
            {
                return;
            }

            isClosingEventInitialized = true;
            shellView.Value.Closing += ShellViewClosing;
        }

        private void ShellViewClosing(object sender, CancelEventArgs e)
        {
            OnClosing(e);
        }

        public void InitializeOAuthManager()
        {
            OAuthManager["consumer_key"] = Settings.ApiKey;
            OAuthManager["consumer_secret"] = Settings.SecretKey;
            OAuthManager["token"] = Settings.OAuthToken;
            OAuthManager["token_secret"] = Settings.OAuthTokenSecret;
        }
    }
}
