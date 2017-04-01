using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TumblThree.Domain.Models
{
    public interface IFiles : INotifyPropertyChanged
    {
        IList<string> Links { get; set; }

        bool Save();
    }
}
