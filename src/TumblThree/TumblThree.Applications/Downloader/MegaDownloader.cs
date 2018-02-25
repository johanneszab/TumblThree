using CG.Web.MegaApiClient;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TumblThree.Applications.Downloader
{
    public class MegaDownloader : IMegaDownloader
    {
        private MegaApiClient client;

        public MegaDownloader (MegaApiClient client)
        {
            this.client = client;
        }

        public async Task Login()
        {
            await client.LoginAnonymousAsync();
        }

        public async Task Logout()
        {
            await client.LogoutAsync();
        }

        public async Task<Stream> DownloadAsync(string url)
        {

            Uri link = new Uri(url);
            Progress<double> megaProgress = new Progress<double>();

            return await client.DownloadAsync(link, megaProgress);
        }
    }
}
