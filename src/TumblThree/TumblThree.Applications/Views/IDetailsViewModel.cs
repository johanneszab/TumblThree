using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Views
{
    public interface IDetailsViewModel
    {
        IBlog BlogFile { get; set; }

        int Count { get; set; }

        object View { get; }
    }
}
