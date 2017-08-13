using System;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;

namespace TumblThree.Applications.Downloader
{
    public interface IDownloader
    {
        Task Crawl();

        Task IsBlogOnlineAsync();

        Task UpdateMetaInformationAsync();
    }
}
