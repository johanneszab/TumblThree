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

        [ImportingConstructor]
        public ManagerService()
        {
            blogFiles = new ObservableCollection<IBlog>();
        }

        public ObservableCollection<IBlog> BlogFiles
        {
            get { return blogFiles; }
        }
    }
}
