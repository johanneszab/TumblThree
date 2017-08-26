using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for ManagerView.xaml
    /// </summary>
    [Export(typeof(IManagerView))]
    public partial class ManagerView : IManagerView
    {
        private readonly Lazy<ManagerViewModel> viewModel;

        public ManagerView()
        {
            InitializeComponent();
            viewModel = new Lazy<ManagerViewModel>(() => ViewHelper.GetViewModel<ManagerViewModel>(this));

            Loaded += LoadedHandler;
            blogFilesGrid.Sorting += BlogFilesGridSorting;
            blogFilesGrid.SelectionChanged += SelectionChanged;
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectionService.AddRange(e.AddedItems.Cast<IBlog>());
            ViewModel.SelectionService.RemoveRange(e.RemovedItems.Cast<IBlog>());
        }

        private ManagerViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        public Dictionary<object, Tuple<int, double, Visibility>> DataGridColumnRestore
        {
            get
            {
                var columnSettings = new Dictionary<object, Tuple<int, double, Visibility>>();
                foreach (DataGridColumn column in blogFilesGrid.Columns)
                {
                    columnSettings.Add(column.Header, Tuple.Create(column.DisplayIndex, column.Width.Value, column.Visibility));
                }
                return columnSettings;
            }
            set
            {
                foreach (DataGridColumn column in blogFilesGrid.Columns)
                {
                    Tuple<int, double, Visibility> entry;
                    value.TryGetValue(column.Header, out entry);
                    column.DisplayIndex = entry.Item1;
                    column.Width = new DataGridLength(entry.Item2, DataGridLengthUnitType.Pixel);
                    column.Visibility = entry.Item3;
                }
            }
        }

        private void LoadedHandler(object sender, RoutedEventArgs e)
        {
            FocusBlogFilesGrid();
        }

        private void BlogFilesGridSorting(object sender, DataGridSortingEventArgs e)
        {
            var collectionView = CollectionViewSource.GetDefaultView(blogFilesGrid.ItemsSource) as ListCollectionView;
            if (collectionView == null)
            {
                return;
            }
            ListSortDirection newDirection = e.Column.SortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }

        private void FocusBlogFilesGrid()
        {
            blogFilesGrid.Focus();
            blogFilesGrid.CurrentCell = new DataGridCellInfo(blogFilesGrid.SelectedItem, blogFilesGrid.Columns[0]);
        }

        private void DataGridRowContextMenuOpening(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).ContextMenu.DataContext = ViewModel;
        }

        private void DataGridRowMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.CrawlerService.EnqueueSelectedCommand.CanExecute(null))
                ViewModel.CrawlerService.EnqueueSelectedCommand.Execute(null);
        }

        private void DataGridRowMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var draggedItem = (DataGridRow)sender;

                if (draggedItem.IsEditing)
                    return;

                List<QueueListItem> items = blogFilesGrid.ItemsSource.Cast<IBlog>().Select(x => new QueueListItem(x)).ToList();
                IEnumerable<QueueListItem> selectedItems = blogFilesGrid.SelectedItems
                    .Cast<IBlog>()
                    .OrderBy(x => items.IndexOf(new QueueListItem(x)))
                    .Select(x => new QueueListItem(x));
                DragDrop.DoDragDrop(draggedItem, selectedItems.Select(x => x.Blog).ToArray(), DragDropEffects.Copy);
            }
        }
    }
}
