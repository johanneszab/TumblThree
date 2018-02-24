using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Waf.Foundation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace TumblThree.Presentation.Controls
{
    public class ListBoxDragDropHelper<TItem>
    {
        private readonly Action<int, IEnumerable> insertItemsAction;
        private readonly InsertMarkerAdorner insertMarkerAdorner;
        private readonly ListBox listBox;
        private readonly Action<int, IEnumerable<TItem>> moveItemsAction;
        private readonly ThrottledAction throttledAutoScrollAction;
        private readonly Func<DragEventArgs, IEnumerable> tryGetInsertItemsAction;
        private ListBoxItem dragSource;
        private DragEventArgs lastPreviewDragOverEventArgs;
        private Point? startPoint;

        public ListBoxDragDropHelper(ListBox listBox, Action<int, IEnumerable<TItem>> moveItemsAction,
            Func<DragEventArgs, IEnumerable> tryGetInsertItemsAction, Action<int, IEnumerable> insertItemsAction)
        {
            this.listBox = listBox;
            this.moveItemsAction = moveItemsAction;
            this.tryGetInsertItemsAction = tryGetInsertItemsAction ?? (eventArgs => null);
            this.insertItemsAction = insertItemsAction;
            insertMarkerAdorner = new InsertMarkerAdorner(listBox);
            throttledAutoScrollAction = new ThrottledAction(ThrottledAutoScroll, ThrottledActionMode.InvokeMaxEveryDelayTime,
                TimeSpan.FromMilliseconds(250));

            listBox.Loaded += ListBoxLoaded;
            if (listBox.IsLoaded)
            {
                InitializeAdornerLayer();
            }

            listBox.PreviewDragOver += ListBoxPreviewDragOver;
            listBox.Drop += ListBoxDrop;

            Style listboxItemStyle = new Style(typeof(ListBoxItem), listBox.ItemContainerStyle);
            listboxItemStyle.Setters.Add(new EventSetter(UIElement.PreviewMouseLeftButtonDownEvent,
                (MouseButtonEventHandler)ListBoxItemPreviewMouseLeftButtonDown));
            listboxItemStyle.Setters.Add(new EventSetter(UIElement.PreviewMouseMoveEvent,
                (MouseEventHandler)ListBoxItemPreviewMouseMove));
            listboxItemStyle.Setters.Add(new EventSetter(UIElement.DragEnterEvent, (DragEventHandler)ListBoxItemDragEnter));
            listboxItemStyle.Setters.Add(new EventSetter(UIElement.DragLeaveEvent, (DragEventHandler)ListBoxItemDragLeave));
            listboxItemStyle.Setters.Add(new EventSetter(UIElement.DropEvent, (DragEventHandler)ListBoxItemDrop));
            listBox.ItemContainerStyle = listboxItemStyle;
        }

        private void InitializeAdornerLayer()
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(listBox);
            adornerLayer.Add(insertMarkerAdorner);
        }

        private void ListBoxLoaded(object sender, RoutedEventArgs e)
        {
            InitializeAdornerLayer();
        }

        private void ListBoxItemPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }

        private void ListBoxItemPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if ((moveItemsAction != null) && (e.LeftButton == MouseButtonState.Pressed) && (startPoint != null))
            {
                Point position = e.GetPosition(null);
                if ((Math.Abs(position.X - startPoint.Value.X) < SystemParameters.MinimumHorizontalDragDistance)
                    && (Math.Abs(position.Y - startPoint.Value.Y) < SystemParameters.MinimumVerticalDragDistance))
                {
                    return;
                }

                ListBoxItem target = (ListBoxItem)sender;
                List<TItem> items = listBox.Items.Cast<TItem>().ToList();
                TItem[] selectedItems = listBox.SelectedItems.Cast<TItem>().OrderBy(x => items.IndexOf(x)).ToArray();

                dragSource = target;
                DragDrop.DoDragDrop(target, selectedItems, DragDropEffects.Move);
                insertMarkerAdorner.ResetMarker();
                dragSource = null;
                startPoint = null;
            }
        }

        private void ListBoxPreviewDragOver(object sender, DragEventArgs e)
        {
            if (!CanMoveItems(e) && !CanInsertItems(e))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            lastPreviewDragOverEventArgs = e;
            throttledAutoScrollAction.InvokeAccumulated();
        }

        private void ThrottledAutoScroll()
        {
            ScrollViewer scrollViewer = FindVisualChild<ScrollViewer>(listBox);
            const double tolerance = 15;
            const double offset = 3;
            double delta = 0;
            double verticalPosition = lastPreviewDragOverEventArgs.GetPosition(listBox).Y;

            if (verticalPosition < tolerance)
            {
                delta = -offset;
            }
            else if (verticalPosition > listBox.ActualHeight - tolerance)
            {
                delta = +offset;
            }

            if (delta != 0)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + delta);
            }
        }

        private void ListBoxItemDragEnter(object sender, DragEventArgs e)
        {
            if (!CanMoveItems(e) && !CanInsertItems(e))
            {
                return;
            }

            ListBoxItem target = (ListBoxItem)sender;
            if ((dragSource != null)
                && (dragSource.TranslatePoint(new Point(), listBox).Y > target.TranslatePoint(new Point(), listBox).Y))
            {
                insertMarkerAdorner.ShowMarker(target, false);
            }
            else
            {
                insertMarkerAdorner.ShowMarker(target, true);
            }
        }

        private void ListBoxItemDragLeave(object sender, DragEventArgs e)
        {
            insertMarkerAdorner.ResetMarker();
        }

        private void ListBoxItemDrop(object sender, DragEventArgs e)
        {
            insertMarkerAdorner.ResetMarker();
            if (!CanMoveItems(e) && !CanInsertItems(e))
            {
                return;
            }
            object targetData = ((ListBoxItem)sender).DataContext;
            int newIndex = listBox.Items.IndexOf(targetData);

            if (e.Effects == DragDropEffects.Move)
            {
                moveItemsAction(newIndex, (TItem[])e.Data.GetData(typeof(TItem[])));
            }
            else if (e.Effects.HasFlag(DragDropEffects.Copy))
            {
                IEnumerable droppedData = tryGetInsertItemsAction(e);
                insertItemsAction(newIndex + 1, droppedData);
                SelectItems(newIndex + 1, droppedData.Cast<object>().Count());
            }

            FocusSelectedItem();
            e.Handled = true;
        }

        private void ListBoxDrop(object sender, DragEventArgs e)
        {
            insertMarkerAdorner.ResetMarker();
            if (!CanMoveItems(e) && !CanInsertItems(e))
            {
                return;
            }
            int newIndex = listBox.Items.Count - 1;

            if (e.Effects == DragDropEffects.Move)
            {
                moveItemsAction(newIndex, (TItem[])e.Data.GetData(typeof(TItem[])));
            }
            else if (e.Effects.HasFlag(DragDropEffects.Copy))
            {
                IEnumerable droppedData = tryGetInsertItemsAction(e);
                insertItemsAction(newIndex + 1, droppedData);
                SelectItems(newIndex + 1, droppedData.Cast<object>().Count());
            }

            FocusSelectedItem();
            e.Handled = true;
        }

        private bool CanMoveItems(DragEventArgs e)
        {
            if (moveItemsAction == null)
            {
                return false;
            }
            TItem[] items = e.Data.GetData(typeof(TItem[])) as TItem[];
            return (items != null) && items.Any();
        }

        private bool CanInsertItems(DragEventArgs e)
        {
            IEnumerable items = tryGetInsertItemsAction(e);
            return (items != null) && items.Cast<object>().Any();
        }

        private void SelectItems(int index, int count)
        {
            IEnumerable<object> items = listBox.Items.Cast<object>().Skip(index).Take(count);
            listBox.SelectedItems.Clear();
            foreach (object item in items)
            {
                listBox.SelectedItems.Add(item);
            }
        }

        private void FocusSelectedItem()
        {
            if ((Keyboard.FocusedElement == listBox) && (listBox.SelectedItems.Count > 1))
            {
                // This happens when moving multiple items at once. If we set the focus now then it clears the selection.
                return;
            }

            listBox.Dispatcher.InvokeAsync(() =>
            {
                ListBoxItem listBoxItem = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromItem(listBox.SelectedItem);
                if (listBoxItem != null)
                {
                    listBoxItem.Focus();
                }
            }, DispatcherPriority.Background);
        }

        private static TChild FindVisualChild<TChild>(DependencyObject obj) where TChild : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is TChild)
                {
                    return (TChild)child;
                }
                else
                {
                    TChild childOfChild = FindVisualChild<TChild>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }
    }
}
