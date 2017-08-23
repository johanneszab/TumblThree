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
        private readonly Lazy<ICrawlerService> crawlerService;
        private readonly Lazy<IManagerService> managerService;
        private readonly Lazy<ISelectionService> selectionService;
        private Blog selectedBlogFile;
        private ICommand showDetailsCommand;
        private ICommand showFilesCommand;
        private ICommand visitBlogCommand;

        [ImportingConstructor]
        public ManagerViewModel(IManagerView view, IShellService shellService, Lazy<ISelectionService> selectionService,
            Lazy<ICrawlerService> crawlerService, Lazy<IManagerService> managerService) : base(view)
        {
            ShellService = shellService;
            this.selectionService = selectionService;
            this.crawlerService = crawlerService;
            this.managerService = managerService;

            try
            {
                if (shellService.Settings.ColumnSettings.Count != 0)
                {
                    view.DataGridColumnRestore = ShellService.Settings.ColumnSettings;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ManagerViewModel:ManagerViewModel {0}", ex);
                shellService.ShowError(ex, Resources.CouldNotRestoreUISettings);
                return;
            }

            ShellService.Closing += ViewClosed;
        }

        public ISelectionService SelectionService
        {
            get { return selectionService.Value; }
        }

        public IShellService ShellService { get; }

        public ICrawlerService CrawlerService
        {
            get { return crawlerService.Value; }
        }

        public IManagerService ManagerService
        {
            get { return managerService.Value; }
        }

        public ICommand ShowFilesCommand
        {
            get { return showFilesCommand; }
            set { SetProperty(ref showFilesCommand, value); }
        }

        public ICommand VisitBlogCommand
        {
            get { return visitBlogCommand; }
            set { SetProperty(ref visitBlogCommand, value); }
        }

        public ICommand ShowDetailsCommand
        {
            get { return showDetailsCommand; }
            set { SetProperty(ref showDetailsCommand, value); }
        }

        public Blog SelectedBlogFile
        {
            get { return selectedBlogFile; }
            set { SetProperty(ref selectedBlogFile, value); }
        }

        public IReadOnlyObservableList<QueueListItem> QueueItems { get; set; }

        public void ViewClosed(object sender, EventArgs e)
        {
            ShellService.Settings.ColumnSettings = ViewCore.DataGridColumnRestore;
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
