using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Waf.Foundation;
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Services
{
    [Export, Export(typeof(ISelectionService))]
    internal class SelectionService : Model, ISelectionService
    {
        private readonly ObservableCollection<IBlog> selectedBlogFiles;

        [ImportingConstructor]
        public SelectionService()
        {
            this.selectedBlogFiles = new ObservableCollection<IBlog>();
        }

        public IList<IBlog> SelectedBlogFiles { get { return selectedBlogFiles; } }
    }
}
