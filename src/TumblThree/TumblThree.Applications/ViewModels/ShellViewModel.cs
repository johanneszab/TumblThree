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
        private readonly DelegateCommand closeErrorCommand;
        private readonly DelegateCommand exitCommand;
        private readonly DelegateCommand garbageCollectorCommand;
        private readonly DelegateCommand showAboutCommand;
        private readonly DelegateCommand showSettingsCommand;

        private readonly ExportFactory<AboutViewModel> aboutViewModelFactory;
        private readonly ObservableCollection<Tuple<Exception, string>> errors;
        private readonly AppSettings settings;
        private readonly ExportFactory<SettingsViewModel> settingsViewModelFactory;

        private object detailsView;

        [ImportingConstructor]
        public ShellViewModel(IShellView view, IShellService shellService, ICrawlerService crawlerService,
            ExportFactory<SettingsViewModel> settingsViewModelFactory,
            ExportFactory<AboutViewModel> aboutViewModelFactory)
            : base(view)
        {
            ShellService = shellService;
            CrawlerService = crawlerService;
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

        public string Title => ApplicationInfo.ProductName;

        public IShellService ShellService { get; }

        public ICrawlerService CrawlerService { get; }

        public IReadOnlyList<Tuple<Exception, string>> Errors => errors;

        public Tuple<Exception, string> LastError => errors.LastOrDefault();

        public ICommand ExitCommand => exitCommand;

        public ICommand CloseErrorCommand => closeErrorCommand;

        public ICommand GarbageCollectorCommand => garbageCollectorCommand;

        public ICommand ShowSettingsCommand => showSettingsCommand;

        public ICommand ShowAboutCommand => showAboutCommand;

        public object DetailsView
        {
            get => detailsView;
            private set => SetProperty(ref detailsView, value);
        }

        public bool IsDetailsViewVisible
        {
            get => DetailsView == ShellService.DetailsView;
            set
            {
                if (value)
                {
                    DetailsView = ShellService.DetailsView;
                }
            }
        }

        public bool IsQueueViewVisible
        {
            get => DetailsView == ShellService.QueueView;
            set
            {
                if (value)
                {
                    DetailsView = ShellService.QueueView;
                }
            }
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
            SettingsViewModel settingsViewModel = settingsViewModelFactory.CreateExport().Value;
            settingsViewModel.ShowDialog(ShellService.ShellView);
        }

        public void ShowAboutView()
        {
            AboutViewModel aboutViewModel = aboutViewModelFactory.CreateExport().Value;
            aboutViewModel.ShowDialog(ShellService.ShellView);
        }

        public void ShowError(Exception exception, string message)
        {
            var errorMessage = new Tuple<Exception, string>(exception, message);
            if (
                !errors.Any(
                    error =>
                        (error.Item1?.ToString() ?? "null") == (errorMessage.Item1?.ToString() ?? "null") &&
                        error.Item2 == errorMessage.Item2))
            {
                errors.Add(errorMessage);
            }
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
