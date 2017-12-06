using System.Collections.ObjectModel;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Services
{
    public interface IManagerService
    {
        ObservableCollection<IBlog> BlogFiles { get; }

        ObservableCollection<IFiles> Databases { get; }

        bool CheckIfFileExistsInDB(string url);
    }
}
