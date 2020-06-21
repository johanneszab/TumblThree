using System;
using System.Threading.Tasks;

namespace TumblThree.Applications.Crawler
{
    public interface ICrawler : IDisposable
    {
        Task CrawlAsync();

        Task IsBlogOnlineAsync();

        Task UpdateMetaInformationAsync();
    }
}
