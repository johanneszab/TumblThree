using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Waf.Applications;
using System.Windows.Input;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class ShellViewModel : ViewModel<IShellView>
    {
        private readonly AppSettings settings;
        private readonly ObservableCollection<Tuple<Exception, string>> errors;
        private readonly ExportFactory<SettingsViewModel> settingsViewModelFactory;
        private readonly ExportFactory<AboutViewModel> aboutViewModelFactory;
        private readonly DelegateCommand exitCommand;
        private readonly DelegateCommand closeErrorCommand;
        private readonly DelegateCommand garbageCollectorCommand;
        private readonly DelegateCommand showSettingsCommand;
        private readonly DelegateCommand showAboutCommand;

        private object detailsView;


        [ImportingConstructor]
        public ShellViewModel(IShellView view, IShellService shellService, ICrawlerService crawlerService, ISelectionService selectionService, ExportFactory<SettingsViewModel> settingsViewModelFactory,
            ExportFactory<AboutViewModel> aboutViewModelFactory)
            : base(view)
        {
            ShellService = shellService;
            CrawlerService = crawlerService;
            SelectionService = selectionService;
            settings = shellService.Settings;
            this.settingsViewModelFactory = settingsViewModelFactory;
            this.aboutViewModelFactory = aboutViewModelFactory;
            errors = new ObservableCollection<Tuple<Exception, string>>();
            exitCommand = new DelegateCommand(Close);
            closeErrorCommand = new DelegateCommand(CloseError);
            garbageCollectorCommand = new DelegateCommand(GC.Collect);
            showSettingsCommand = new DelegateCommand(ShowSettingsView);
            showAboutCommand = new DelegateCommand(ShowAboutView);


            errors.CollectionChanged += ErrorsCollectionChanged;
            view.Closed += ViewClosed;

            // Restore the window size when the values are valid.
            if (settings.Left >= 0 && settings.Top >= 0 && settings.Width > 0 && settings.Height > 0
                && settings.Left + settings.Width <= view.VirtualScreenWidth
                && settings.Top + settings.Height <= view.VirtualScreenHeight)
            {
                view.Left = settings.Left;
                view.Top = settings.Top;
                view.Height = settings.Height;
                view.Width = settings.Width;
                view.GridSplitterPosition = settings.GridSplitterPosition;
            }
            view.IsMaximized = settings.IsMaximized;
        }

        public string Title { get { return ApplicationInfo.ProductName; } }

        public IShellService ShellService { get; }

        public ICrawlerService CrawlerService { get; }

        public ISelectionService SelectionService { get; }

        public IReadOnlyList<Tuple<Exception, string>> Errors { get { return errors; } }

        public Tuple<Exception, string> LastError { get { return errors.LastOrDefault(); } }

        public ICommand ExitCommand { get { return exitCommand; } }

        public ICommand CloseErrorCommand { get { return closeErrorCommand; } }

        public ICommand GarbageCollectorCommand { get { return garbageCollectorCommand; } }

        public ICommand ShowSettingsCommand { get { return showSettingsCommand; } }

        public ICommand ShowAboutCommand { get { return showAboutCommand; } }

        public object DetailsView
        {
            get { return detailsView; }
            private set { SetProperty(ref detailsView, value); }
        }

        public bool IsDetailsViewVisible
        {
            get { return DetailsView == ShellService.DetailsView; }
            set { if (value) { DetailsView = ShellService.DetailsView; } }
        }

        public bool IsQueueViewVisible
        {
            get { return DetailsView == ShellService.QueueView; }
            set { if (value) { DetailsView = ShellService.QueueView; } }
        }

        public void Show()
        {
            ViewCore.Show();
        }

        private void Close()
        {
            ViewCore.Close();
        }

        public void ShowSettingsView()
        {
            var settingsViewModel = settingsViewModelFactory.CreateExport().Value;
            settingsViewModel.ShowDialog(ShellService.ShellView);
        }

        public void ShowAboutView()
        {
            var aboutViewModel = aboutViewModelFactory.CreateExport().Value;
            aboutViewModel.ShowDialog(ShellService.ShellView);
        }

        public void ShowError(Exception exception, string message)
        {
            string ErrorMessageItem1;
            var errorMessage = new Tuple<Exception, string>(exception, message);
            ErrorMessageItem1 = errorMessage.Item1?.ToString() ?? "";
            if (!errors.Any(error => error.Item1.ToString() == ErrorMessageItem1 && error.Item2 == errorMessage.Item2))
                errors.Add(errorMessage);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(DetailsView))
            {
                RaisePropertyChanged(nameof(IsDetailsViewVisible));
                RaisePropertyChanged(nameof(IsQueueViewVisible));
            }
        }

        private void CloseError()
        {
            if (errors.Any())
            {
                errors.RemoveAt(errors.Count - 1);
            }
        }

        private void ErrorsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(LastError));
        }

        private void ViewClosed(object sender, EventArgs e)
        {
            settings.Left = ViewCore.Left;
            settings.Top = ViewCore.Top;
            settings.Height = ViewCore.Height;
            settings.Width = ViewCore.Width;
            settings.IsMaximized = ViewCore.IsMaximized;
            settings.GridSplitterPosition = ViewCore.GridSplitterPosition;
        }
    }
}
