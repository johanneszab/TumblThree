using System.Collections.Generic;

using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Services
{
    public interface IDetailsService
    {
        void SelectBlogFiles(IReadOnlyList<IBlog> blogFiles);
    }
}
