using System;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;

namespace TumblThree.Applications.Crawler
{
    public interface ICrawler
    {
        Task Crawl();

        Task IsBlogOnlineAsync();

        Task UpdateMetaInformationAsync();

        void UpdateProgressQueueInformation(string format, params object[] args);
    }
}
