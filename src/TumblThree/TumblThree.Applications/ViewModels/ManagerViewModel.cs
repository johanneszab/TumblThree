using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Waf.Applications;
using System.Windows.Input;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using DataModels = TumblThree.Applications.DataModels;
using TumblThree.Domain;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class ManagerViewModel : ViewModel<IManagerView>
    {
        private readonly Lazy<ISelectionService> selectionService;
        private readonly ObservableCollection<Blog> selectedManagerItems;
        private Blog selectedBlogFile;
        private readonly ICrawlerService crawlerService;
        private ICommand showFilesCommand;
        private ICommand visitBlogCommand;

        

        [ImportingConstructor]
        public ManagerViewModel(IManagerView view, IShellService shellService, Lazy<ISelectionService> selectionService, ICrawlerService crawlerService) : base(view)
        {
            ShellService = shellService;
            this.selectionService = selectionService;
            this.selectedManagerItems = new ObservableCollection<Blog>();
            this.crawlerService = crawlerService;

            if (shellService.Settings.ColumnWidths.Count != 0)
                view.DataGridColumnRestore = ShellService.Settings.ColumnWidths;

            ShellService.Closing += ViewClosed;
        }

        public void ViewClosed(object sender, EventArgs e)
        {
            ShellService.Settings.ColumnWidths = ViewCore.DataGridColumnRestore;
        }

        public ISelectionService SelectionService { get { return selectionService.Value; } }

        public IShellService ShellService { get; }

        public ICrawlerService CrawlerService { get { return crawlerService; } }

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

        public Blog SelectedBlogFile
        {
            get { return selectedBlogFile; }
            set { SetProperty(ref selectedBlogFile, value); }
        }
    }
}
