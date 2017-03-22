using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TumblThree.Domain.Models
{
    public interface IBlog : INotifyPropertyChanged
    {
        string Name { get; set; }

        string Url { get; set; }

        string Location { get; set; }

        string ChildId { get; set; }

        int Rating { get; set; }

        int DownloadedImages { get; set; }

        int TotalCount { get; set; }

        string Notes { get; set; }

        BlogTypes BlogType { get; set; }

        DateTime DateAdded { get; set; }

        DateTime LastCompleteCrawl { get; set; }

        bool Online { get; set; }

        bool CheckDirectoryForFiles { get; set; }

        bool Dirty { get; set; }

        Exception LoadError { get; set; }

        IList<string> Links { get; set; }
    }
}
