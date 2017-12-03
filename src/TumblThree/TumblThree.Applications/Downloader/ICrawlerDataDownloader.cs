using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TumblThree.Applications.Downloader
{
    public interface ICrawlerDataDownloader
    {
        Task DownloadCrawlerDataAsync();
    }
}
