using System;
using System.Threading.Tasks;

namespace TumblThree.Applications.Services
{
    public interface IApplicationUpdateService
    {
        Task<string> GetLatestReleaseFromServer();

        bool IsNewVersionAvailable();

        string GetNewAvailableVersion();

        Uri GetDownloadUri();
    }
}
