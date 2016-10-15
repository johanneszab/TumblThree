using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;
using TumblThree.Presentation.Controls;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    /// Interaction logic for QueueView.xaml
    /// </summary>
    [Export(typeof(IQueueView))]
    public partial class QueueView : IQueueView
    {
        private readonly Lazy<QueueViewModel> viewModel;
        private readonly ListBoxDragDropHelper<QueueListItem> listBoxDragDropHelper;

        public QueueView()
        {
            InitializeComponent();
            this.viewModel = new Lazy<QueueViewModel>(() => ViewHelper.GetViewModel<QueueViewModel>(this));
            listBoxDragDropHelper = new ListBoxDragDropHelper<QueueListItem>(queueListBox, MoveItems, TryGetInsertItems, InsertItems);
            //Loaded += FirstTimeLoadedHandler;

        }

        private QueueViewModel ViewModel { get { return viewModel.Value; } }

        //private void FirstTimeLoadedHandler(object sender, RoutedEventArgs e)
        //{
        //    Loaded -= FirstTimeLoadedHandler;
        //    //ViewModel.QueueManager.PropertyChanged += QueueManagerPropertyChanged;
        //}

        private void ListBoxItemContextMenuOpening(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).ContextMenu.DataContext = ViewModel;
        }

        //private void ListBoxItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    ViewModel.CrawlSelectedCommand.Execute(null);
        //}

        private void StatusBarButtonClick(object sender, RoutedEventArgs e)
        {
            menuPopup.Width = statusBarButton.ActualWidth;
            menuPopup.IsOpen = true;
        }

        private void MoveItems(int newIndex, IEnumerable<QueueListItem> itemsToMove)
        {
            ViewModel.QueueManager.MoveItems(newIndex, itemsToMove);
        }

        private IEnumerable TryGetInsertItems(DragEventArgs e)
        {
            return e.Data.GetData(DataFormats.FileDrop) as IEnumerable ?? e.Data.GetData(typeof(IBlog[])) as IEnumerable;
        }

        private void InsertItems(int index, IEnumerable itemsToInsert)
        {
            if (itemsToInsert is IEnumerable<IBlog>)
            {
                ViewModel.InsertBlogFilesAction(index, (IEnumerable<IBlog>)itemsToInsert);
            }
        }
    }
}
