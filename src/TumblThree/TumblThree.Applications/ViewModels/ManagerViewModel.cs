using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Waf.Applications;
using System.Windows.Input;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Applications.DataModels;
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
        public ManagerViewModel(IManagerView view, Lazy<ISelectionService> selectionService, ICrawlerService crawlerService) : base(view)
        {
            this.selectionService = selectionService;
            this.selectedManagerItems = new ObservableCollection<Blog>();
            this.crawlerService = crawlerService;
        }

        public ISelectionService SelectionService { get { return selectionService.Value; } }


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
