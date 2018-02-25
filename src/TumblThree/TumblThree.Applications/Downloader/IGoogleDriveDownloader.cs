using System.Collections.Generic;
using Google.Apis.Drive.v3.Data;
using System.Threading.Tasks;
using GoogleFile = Google.Apis.Drive.v3.Data.File;

namespace TumblThree.Applications.Downloader
{
    public interface IGoogleDriveDownloader
    {
        void Authenticate();
        Task downloadFile(File FileResource, string path);
        string getIdFromUrl(string url);
        List<File> IterateFolder(string folderId);
        void OpenService();
        Task<GoogleFile> RequestInfo(string url, string path);
    }
}