using System;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Waf.Foundation;
using System.Windows.Input;

using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class ManagerViewModel : ViewModel<IManagerView>
    {
        private ICommand copyUrlCommand;
        private ICommand checkStatusCommand;
        private ICommand showDetailsCommand;
        private ICommand showFilesCommand;
        private ICommand visitBlogCommand;

        private readonly Lazy<ICrawlerService> crawlerService;
        private readonly Lazy<IManagerService> managerService;
        private readonly Lazy<ISelectionService> selectionService;
        private Blog selectedBlogFile;

        [ImportingConstructor]
        public ManagerViewModel(IManagerView view, IShellService shellService, Lazy<ISelectionService> selectionService,
            Lazy<ICrawlerService> crawlerService, Lazy<IManagerService> managerService) : base(view)
        {
            ShellService = shellService;
            this.selectionService = selectionService;
            this.crawlerService = crawlerService;
            this.managerService = managerService;

            ShellService.Closing += ViewClosed;
        }

        public ISelectionService SelectionService => selectionService.Value;

        public IShellService ShellService { get; }

        public ICrawlerService CrawlerService => crawlerService.Value;

        public IManagerService ManagerService => managerService.Value;

        public ICommand ShowFilesCommand
        {
            get => showFilesCommand;
            set => SetProperty(ref showFilesCommand, value);
        }

        public ICommand VisitBlogCommand
        {
            get => visitBlogCommand;
            set => SetProperty(ref visitBlogCommand, value);
        }

        public ICommand ShowDetailsCommand
        {
            get => showDetailsCommand;
            set => SetProperty(ref showDetailsCommand, value);
        }

        public ICommand CopyUrlCommand
        {
            get => copyUrlCommand;
            set => SetProperty(ref copyUrlCommand, value);
        }

        public ICommand CheckStatusCommand
        {
            get => checkStatusCommand;
            set => SetProperty(ref checkStatusCommand, value);
        }

        public Blog SelectedBlogFile
        {
            get => selectedBlogFile;
            set => SetProperty(ref selectedBlogFile, value);
        }

        public IReadOnlyObservableList<QueueListItem> QueueItems { get; set; }

        public void ViewClosed(object sender, EventArgs e)
        {
            ShellService.Settings.ColumnSettings = ViewCore.DataGridColumnRestore;
        }

        public void DataGridColumnRestore()
        {
            try
            {
                if (ShellService.Settings.ColumnSettings.Count != 0)
                {
                    ViewCore.DataGridColumnRestore = ShellService.Settings.ColumnSettings;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ManagerViewModel:ManagerViewModel {0}", ex);
                ShellService.ShowError(ex, Resources.CouldNotRestoreUISettings);
                return;
            }
        }

        public void QueueItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                RaisePropertyChanged("QueueItems");
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                RaisePropertyChanged("QueueItems");
            }
        }
    }
}
