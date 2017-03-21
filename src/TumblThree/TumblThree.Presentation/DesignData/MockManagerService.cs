using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Waf.Foundation;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Presentation.DesignData
{
    public class MockManagerService : IManagerService
    {
        private ObservableCollection<IBlog> innerBlogFiles;
        private ObservableCollection<IBlog> blogFiles;

        public MockManagerService()
        {
            this.innerBlogFiles = new ObservableCollection<IBlog>();
            this.blogFiles = new ObservableCollection<IBlog>(innerBlogFiles);
        }

        public ObservableCollection<IBlog> BlogFiles { get { return blogFiles; } }

        public void SetBlogFiles(IEnumerable<IBlog> blogFilesToAdd)
        {
            innerBlogFiles.Clear();
            blogFilesToAdd.ToList().ForEach(x => innerBlogFiles.Add(x));
        }
    }
}
