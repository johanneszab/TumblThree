using System;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows.Input;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models;
using TumblThree.Domain;
using TumblThree.Applications.Properties;
using TumblThree.Domain.Queue;
using System.Waf.Foundation;
using System.Collections.Specialized;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class ManagerViewModel : ViewModel<IManagerView>
    {
        private readonly Lazy<ISelectionService> selectionService;
        private Blog selectedBlogFile;
        private readonly Lazy<ICrawlerService> crawlerService;
        private readonly Lazy<IManagerService> managerService;
        private ICommand showFilesCommand;
        private ICommand visitBlogCommand;
        private ICommand showDetailsCommand;

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
                if (shellService.Settings.ColumnWidths.Count != 0)
                    view.DataGridColumnRestore = ShellService.Settings.ColumnWidths;
            }
            catch (Exception ex)
            {
                Logger.Error("ManagerController: {0}", ex);
                shellService.ShowError(ex, Resources.CouldNotRestoreUISettings);
                return;
            }

            ShellService.Closing += ViewClosed;
        }

        public void ViewClosed(object sender, EventArgs e)
        {
            ShellService.Settings.ColumnWidths = ViewCore.DataGridColumnRestore;
        }

        public ISelectionService SelectionService { get { return selectionService.Value; } }

        public IShellService ShellService { get; }

        public ICrawlerService CrawlerService { get { return crawlerService.Value; } }

        public IManagerService ManagerService { get { return managerService.Value; } }


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
