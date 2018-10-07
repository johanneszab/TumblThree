using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Services
{
    internal interface ISettingsService
    {
        IBlog TransferGlobalSettingsToBlog(IBlog blog);
    }
}
