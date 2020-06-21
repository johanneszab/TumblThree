using System;
using System.Threading.Tasks;

namespace TumblThree.Applications.Downloader
{
    public interface IDownloader : IDisposable
    {
        Task<bool> DownloadBlogAsync();

        void UpdateProgressQueueInformation(string format, params object[] args);
    }
}
