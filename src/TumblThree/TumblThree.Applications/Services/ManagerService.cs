using System;
using System.Collections.Generic;
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
        private readonly IList<IFiles> databases;
        private readonly object checkFilesLock = new object();
        private readonly object databasesLock = new object();

        [ImportingConstructor]
        public ManagerService()
        {
            blogFiles = new ObservableCollection<IBlog>();
            databases = new List<IFiles>();
        }

        public ObservableCollection<IBlog> BlogFiles
        {
            get { return blogFiles; }
        }

        public IEnumerable<IFiles> Databases
        {
            get { return databases; }
        }

        public bool CheckIfFileExistsInDB(string url)
        {
            lock (checkFilesLock)
            {
                foreach (IFiles db in databases)
                {
                    if (db.CheckIfFileExistsInDB(url))
                        return true;
                }
                return false;
            }
        }

        public void RemoveDatabase(IFiles database)
        {
            lock (databasesLock)
            {
                databases.Remove(database);
            }
        }

        public void AddDatabase(IFiles database)
        {
            lock (databasesLock)
            {
                databases.Add(database);
            }
        }

        public void ClearDatabases()
        {
            lock (databasesLock)
            {
                databases.Clear();
            }
        }
    }
}
