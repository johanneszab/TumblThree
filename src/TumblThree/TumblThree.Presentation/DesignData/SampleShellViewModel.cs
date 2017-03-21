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
        public SampleShellViewModel() : base(new MockShellView(), new MockShellService(), null, new MockSelectionService(), null, null)
        {
            ShellService.QueueView = new Control();
            IsQueueViewVisible = true;
            ShowError(null, "Error Message: Could not load blog: nature-wallpaper");

            var blogFiles = new[]
{
                new TumblrBlog
                {
                    Name = "Nature Wallpapers",
                    LoadError = null,
                    Links = new List<string>(),
                    Tags = "",
                    Online = true,
                    Rating = 50,
                    LastCompleteCrawl = DateTime.Now,
                    Description = "beatiful nature wallpaper",
                    Url = @"http://nature-wallpaper.tumblr.com/",
                    DownloadedImages = 123,
                    DateAdded = DateTime.Now,
                    Progress = 66,
                    TotalCount = 234,
                },
                new TumblrBlog
                {
                    Name = "Landscape Wallpapers",
                    Url = "http://landscape-wallpaper.tumblr.com/",
                    DownloadedImages = 17236,
                    DateAdded = DateTime.Now,
                    Progress = 95,
                    TotalCount = 15739,
                },
                new TumblrBlog
                {
                    Name = "FX Wallpapers",
                    Url = "http://nature-wallpaper.tumblr.com/",
                    DownloadedImages = 12845,
                    DateAdded = DateTime.Now,
                    Progress = 12,
                    TotalCount = 82453,
                }
            };
            ((MockSelectionService)SelectionService).SetBlogFiles(blogFiles.ToArray());
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