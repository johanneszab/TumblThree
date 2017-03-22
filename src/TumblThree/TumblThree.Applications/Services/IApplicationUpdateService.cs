using System;

namespace TumblThree.Applications.Services
{
    public interface IApplicationUpdateService
    {
        string GetLatestReleaseFromServer();

        bool IsNewVersionAvailable();

        string GetNewAvailableVersion();

        Uri GetDownloadUri();
    }
}
