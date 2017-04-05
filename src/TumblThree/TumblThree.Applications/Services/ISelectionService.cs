using System.Collections.Generic;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Services
{
    public interface ISelectionService
    {
        IList<IBlog> SelectedBlogFiles { get; }
    }
}
