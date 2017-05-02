using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models;

namespace TumblThree.Presentation.DesignData
{
    public class SampleManagerViewModel : ManagerViewModel
    {
        public SampleManagerViewModel() : base(new MockManagerView(), new MockShellService(),
            new Lazy<ISelectionService>(() => new MockSelectionService()), null,
            new Lazy<IManagerService>(() => new MockManagerService()))
        {
            var blogFiles = new[]
            {
                new Blog
                {
                    Name = "Nature Wallpapers",
                    DownloadedImages = 123,
                    TotalCount = 234,
                    Progress = 66,
                    Online = true,
                    Tags = "",
                    DateAdded = DateTime.Now,
                    Rating = 33
                },
                new Blog
                {
                    Name = "Landscape Wallpapers",
                    Url = "http://landscape-wallpaper.tumblr.com/",
                    DownloadedImages = 17236,
                    DateAdded = DateTime.Now,
                    Progress = 95,
                    TotalCount = 15739,
                },
                new Blog
                {
                    Name = "FX Wallpapers",
                    Url = "http://nature-wallpaper.tumblr.com/",
                    DownloadedImages = 12845,
                    DateAdded = DateTime.Now,
                    Progress = 12,
                    TotalCount = 82453,
                }
            };
            ((MockManagerService)ManagerService).SetBlogFiles(blogFiles.ToArray());
        }

        private class MockManagerView : MockView, IManagerView
        {
            public Dictionary<object, Tuple<int, double, Visibility>> DataGridColumnRestore { get; set; }
        }
    }
}
