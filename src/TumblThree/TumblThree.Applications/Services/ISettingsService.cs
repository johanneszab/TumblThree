using TumblThree.Domain.Models;

namespace TumblThree.Applications.Services
{
    internal interface ISettingsService
    {
        IBlog TransferGlobalSettingsToBlog(IBlog blog);
    }
}
