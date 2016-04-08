using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows.Input;
using TumblThree.Applications.Views;
using TumblThree.Domain.Queue;
using TumblThree.Domain.Models;
using TumblThree.Applications.Services;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class QueueViewModel : ViewModel<IQueueView>
    {
        private readonly ObservableCollection<QueueListItem> selectedQueueItems;
        private QueueManager queueManager;
        private QueueListItem selectedQueueItem;

        //private ICommand crawlSelectedCommand;
        private ICommand removeSelectedCommand;
        private ICommand showBlogPropertiesCommand;
        private ICommand openQueueCommand;
        private ICommand saveQueueCommand;
        private ICommand clearQueueCommand;


        [ImportingConstructor]
        public QueueViewModel(IQueueView view, ISelectionService selectionService) : base(view)
        {
            this.selectedQueueItems = new ObservableCollection<QueueListItem>();
            SelectionService = selectionService;
        }

        public QueueManager QueueManager
        {
            get { return queueManager; }
            set { SetProperty(ref queueManager, value); }
        }

        public ISelectionService SelectionService { get; }

        public QueueListItem SelectedQueueItem
        {
            get { return selectedQueueItem; }
            set { SetProperty(ref selectedQueueItem, value); }
        }

        public IList<QueueListItem> SelectedQueueItems { get { return selectedQueueItems; } }

        public ICommand RemoveSelectedCommand
        {
            get { return removeSelectedCommand; }
            set { SetProperty(ref removeSelectedCommand, value); }
        }

        public ICommand ShowBlogPropertiesCommand
        {
            get { return showBlogPropertiesCommand; }
            set { SetProperty(ref showBlogPropertiesCommand, value); }
        }

        public ICommand OpenQueueCommand
        {
            get { return openQueueCommand; }
            set { SetProperty(ref openQueueCommand, value); }
        }

        public ICommand SaveQueueCommand
        {
            get { return saveQueueCommand; }
            set { SetProperty(ref saveQueueCommand, value); }
        }

        public ICommand ClearQueueCommand
        {
            get { return clearQueueCommand; }
            set { SetProperty(ref clearQueueCommand, value); }
        }

        public Action<int, IEnumerable<IBlog>> InsertBlogFilesAction { get; set; }

    }
}
