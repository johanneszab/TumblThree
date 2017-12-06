using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Waf.Foundation;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Services
{
    [Export, Export(typeof(IManagerService))]
    internal class ManagerService : Model, IManagerService
    {
        private readonly ObservableCollection<IBlog> blogFiles;
        private readonly ObservableCollection<IFiles> databases;
        private readonly Object checkFilesLock = new Object();

        [ImportingConstructor]
        public ManagerService()
        {
            blogFiles = new ObservableCollection<IBlog>();
            databases = new ObservableCollection<IFiles>();
        }

        public ObservableCollection<IBlog> BlogFiles
        {
            get { return blogFiles; }
        }

        public ObservableCollection<IFiles> Databases
        {
            get { return databases; }
        }

        public bool CheckIfFileExistsInDB(string url)
        {
            lock (checkFilesLock)
            {
                foreach (var db in databases)
                {
                    if (db.CheckIfFileExistsInDB(url))
                        return true;
                }
                return false;
            }
        }
    }
}
