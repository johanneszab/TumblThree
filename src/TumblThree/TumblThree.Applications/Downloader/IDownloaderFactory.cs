using System;
using System.Threading;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Downloader
{
    public interface IDownloaderFactory
    {
        IDownloader GetDownloader(BlogTypes blogTypes);

        IDownloader GetDownloader(BlogTypes blogTypes, CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress, IShellService shellService, ICrawlerService crawlerService, IBlog blog);
    }
}
