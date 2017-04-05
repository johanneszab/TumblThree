using System;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;

namespace TumblThree.Applications.Downloader
{
    public interface IDownloader
    {
        void Crawl(IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt);

        Task IsBlogOnlineAsync();

        Task UpdateMetaInformationAsync();
    }
}
