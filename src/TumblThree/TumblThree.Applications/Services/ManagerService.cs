using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Waf.Foundation;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Services
{
    [Export, Export(typeof(IManagerService))]
    internal class ManagerService : Model, IManagerService
    {
        private readonly ObservableCollection<IBlog> blogFiles;

        [ImportingConstructor]
        public ManagerService()
        {
            this.blogFiles = new ObservableCollection<IBlog>();
        }

        public ObservableCollection<IBlog> BlogFiles { get { return blogFiles; } }
    }
}
