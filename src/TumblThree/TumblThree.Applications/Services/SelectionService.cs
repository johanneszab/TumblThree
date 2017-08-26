using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Waf.Foundation;

using TumblThree.Applications.ObjectModel;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Services
{
    [Export, Export(typeof(ISelectionService))]
    internal class SelectionService : Model, ISelectionService
    {
        private readonly ObservableRangeCollection<IBlog> selectedBlogFiles;

        [ImportingConstructor]
        public SelectionService()
        {
            selectedBlogFiles = new ObservableRangeCollection<IBlog>();
        }

        public IList<IBlog> SelectedBlogFiles
        {
            get { return selectedBlogFiles; }
        }

        public void AddRange(IEnumerable<IBlog> collection)
        {
            selectedBlogFiles.AddRange(collection);
        }

        public void RemoveRange(IEnumerable<IBlog> collection)
        {
            selectedBlogFiles.RemoveRange(collection);
        }
    }
}
