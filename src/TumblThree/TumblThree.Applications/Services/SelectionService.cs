using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Waf.Foundation;

using TumblThree.Applications.ObjectModel;
using TumblThree.Domain.Models.Blogs;

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

        public IList<IBlog> SelectedBlogFiles => selectedBlogFiles;

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
