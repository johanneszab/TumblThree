using TumblThree.Applications.Services;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Downloader
{
    public interface IDownloaderFactory
    {
        IDownloader GetDownloader(BlogTypes blogTypes);

        IDownloader GetDownloader(BlogTypes blogTypes, IShellService shellService, ICrawlerService crawlerService, IBlog blog);
    }
}
