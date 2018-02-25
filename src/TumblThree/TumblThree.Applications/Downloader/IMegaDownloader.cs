using System.IO;
using System.Threading.Tasks;

namespace TumblThree.Applications.Downloader
{
    public interface IMegaDownloader
    {
        Task Login();
        Task Logout();
        Task<Stream> DownloadAsync(string url);
    }
}