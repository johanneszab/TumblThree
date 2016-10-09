using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TumblThree.Domain.Models
{
    public interface IBlog : INotifyPropertyChanged
    {
        string Name { get; set; }

        string Url { get; set; }

        uint Rating { get; set; }

        uint DownloadedImages { get; set; }

        uint TotalCount { get; set; }

        bool Dirty { get; set; }

        string Notes { get; set; }

        DateTime DateAdded { get; set; }

        DateTime LastCompleteCrawl { get; set; }

        bool Online { get; set; }

        Exception LoadError { get; set; }

        IList<string> Links { get; set; }
    }
}
