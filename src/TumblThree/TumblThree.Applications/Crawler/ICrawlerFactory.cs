using System;
using System.Threading;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
    public interface ICrawlerFactory
    {
        ICrawler GetCrawler(IBlog blog);

        ICrawler GetCrawler(IBlog blog, CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress, IShellService shellService, ICrawlerService crawlerService);
    }
}
