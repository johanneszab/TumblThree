using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models;

namespace TumblThree.Presentation.DesignData
{
    public class SampleShellViewModel : ShellViewModel
    {
        public SampleShellViewModel() : base(new MockShellView(), new MockShellService(), new MockCrawlerService(), null, null)
        {
            ShellService.QueueView = new Control();
            IsQueueViewVisible = true;
            ShowError(null, "Error Message: Could not load blog: nature-wallpaper");
        }

        private class MockShellView : MockView, IShellView
        {
            public double VirtualScreenWidth { get { return 0; } }

            public double VirtualScreenHeight { get { return 0; } }

            public double Left { get; set; }

            public double Top { get; set; }

            public double Width { get; set; }

            public double Height { get; set; }

            public bool IsMaximized { get; set; }

            public double GridSplitterPosition { get; set; }

            public event CancelEventHandler Closing;

            public event EventHandler Closed;

            public void Show()
            {
            }

            public void Close()
            {
            }

            protected virtual void OnClosing(CancelEventArgs e)
            {
                Closing?.Invoke(this, e);
            }

            protected virtual void OnClosed(EventArgs e)
            {
                Closed?.Invoke(this, e);
            }
        }
    }
}