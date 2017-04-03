using System.Collections.Generic;
using System.ComponentModel;

namespace TumblThree.Domain.Models
{
    public interface IFiles : INotifyPropertyChanged
    {
        IList<string> Links { get; set; }

        bool Save();
    }
}
