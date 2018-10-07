using System.Collections.Generic;
using System.Collections.ObjectModel;

using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Applications.Services
{
    public interface IManagerService
    {
        ObservableCollection<IBlog> BlogFiles { get; }

        IEnumerable<IFiles> Databases { get; }

        bool CheckIfFileExistsInDB(string url);

        void RemoveDatabase(IFiles database);

        void AddDatabase(IFiles database);

        void ClearDatabases();
    }
}
