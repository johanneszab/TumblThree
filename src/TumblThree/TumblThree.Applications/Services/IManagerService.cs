using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Waf.Foundation;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Services
{
    public interface IManagerService
    {
        ObservableCollection<IBlog> BlogFiles { get; }
    }
}