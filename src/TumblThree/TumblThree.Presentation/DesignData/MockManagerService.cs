using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using TumblThree.Applications.Services;
using TumblThree.Domain.Models;

namespace TumblThree.Presentation.DesignData
{
    public class MockManagerService : IManagerService
    {
        private readonly ObservableCollection<IBlog> blogFiles;
        private readonly ObservableCollection<IBlog> innerBlogFiles;
        private readonly ObservableCollection<IFiles> databases;

        public MockManagerService()
        {
            innerBlogFiles = new ObservableCollection<IBlog>();
            blogFiles = new ObservableCollection<IBlog>(innerBlogFiles);
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

        public void SetBlogFiles(IEnumerable<IBlog> blogFilesToAdd)
        {
            innerBlogFiles.Clear();
            blogFilesToAdd.ToList().ForEach(x => innerBlogFiles.Add(x));
        }

        public bool CheckIfFileExistsInDB(string url)
        {
            return false;
        }
    }
}
