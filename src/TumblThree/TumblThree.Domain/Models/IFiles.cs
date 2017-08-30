using System.Collections.Generic;
using System.ComponentModel;

namespace TumblThree.Domain.Models
{
    public interface IFiles : INotifyPropertyChanged
    {
        IList<string> Links { get; }

        void AddFileToDb(string fileName);

        bool CheckIfFileExistsInDB(string url);

        bool Save();

        IFiles Load(string fileLocation);
    }
}
