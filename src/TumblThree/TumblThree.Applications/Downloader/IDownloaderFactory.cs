using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Downloader
{
    public interface IDownloaderFactory
    {
        IDownloader GetDownloader(BlogTypes blogTypes);
    }
}