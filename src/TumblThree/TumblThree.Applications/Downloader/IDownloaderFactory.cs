using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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