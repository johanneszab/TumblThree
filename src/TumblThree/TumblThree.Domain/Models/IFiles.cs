using System.Collections.Generic;
using System.ComponentModel;

namespace TumblThree.Domain.Models
{
    public interface IFiles : INotifyPropertyChanged
    {
        List<string> Links { get; set; }

        bool Save();

        IFiles Load(string fileLocation);
    }
}
