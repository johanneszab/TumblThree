using System.Threading.Tasks;

namespace TumblThree.Applications.Downloader
{
    public interface IDownloader
    {
        Task Crawl();

        Task IsBlogOnlineAsync();

        Task UpdateMetaInformationAsync();
    }
}
