using System;
using System.Threading;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
    public interface ICrawlerFactory
    {
        ICrawler GetCrawler(BlogTypes blogTypes);

        ICrawler GetCrawler(BlogTypes blogTypes, CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress, IShellService shellService, ICrawlerService crawlerService, IBlog blog);
    }
}
