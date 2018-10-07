using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows.Input;

using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class QueueViewModel : ViewModel<IQueueView>
    {
        private ICommand clearQueueCommand;
        private ICommand openQueueCommand;
        private ICommand removeSelectedCommand;
        private ICommand saveQueueCommand;
        private ICommand showBlogDetailsCommand;

        private QueueManager queueManager;
        private QueueListItem selectedQueueItem;
        private readonly ObservableCollection<QueueListItem> selectedQueueItems;

        [ImportingConstructor]
        public QueueViewModel(IQueueView view, ICrawlerService crawlerService) : base(view)
        {
            selectedQueueItems = new ObservableCollection<QueueListItem>();
            CrawlerService = crawlerService;
        }

        public QueueManager QueueManager
        {
            get => queueManager;
            set => SetProperty(ref queueManager, value);
        }

        public ICrawlerService CrawlerService { get; }

        public QueueListItem SelectedQueueItem
        {
            get => selectedQueueItem;
            set => SetProperty(ref selectedQueueItem, value);
        }

        public IList<QueueListItem> SelectedQueueItems => selectedQueueItems;

        public ICommand RemoveSelectedCommand
        {
            get => removeSelectedCommand;
            set => SetProperty(ref removeSelectedCommand, value);
        }

        public ICommand ShowBlogDetailsCommand
        {
            get => showBlogDetailsCommand;
            set => SetProperty(ref showBlogDetailsCommand, value);
        }

        public ICommand OpenQueueCommand
        {
            get => openQueueCommand;
            set => SetProperty(ref openQueueCommand, value);
        }

        public ICommand SaveQueueCommand
        {
            get => saveQueueCommand;
            set => SetProperty(ref saveQueueCommand, value);
        }

        public ICommand ClearQueueCommand
        {
            get => clearQueueCommand;
            set => SetProperty(ref clearQueueCommand, value);
        }

        public Action<int, IEnumerable<IBlog>> InsertBlogFilesAction { get; set; }
    }
}
