using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Downloader
{
    public class TumblrDownloader : AbstractDownloader
    {
        protected List<string> tags = new List<string>();
        protected int numberOfPagesCrawled = 0;

        public TumblrDownloader(IShellService shellService, IManagerService managerService, CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress, IPostQueue<TumblrPost> postQueue, FileDownloader fileDownloader, ICrawlerService crawlerService, IBlog blog, IFiles files)
            : base(shellService, managerService, ct, pt, progress, postQueue, fileDownloader, crawlerService, blog, files)
        {
        }

        protected string ImageSize()
        {
            if (shellService.Settings.ImageSize == "raw")
                return "1280";
            return shellService.Settings.ImageSize;
        }

        protected string ResizeTumblrImageUrl(string imageUrl)
        {
            var sb = new StringBuilder(imageUrl);
            return sb
                .Replace("_raw", "_" + ImageSize())
                .Replace("_1280", "_" + ImageSize())
                .Replace("_540", "_" + ImageSize())
                .Replace("_500", "_" + ImageSize())
                .Replace("_400", "_" + ImageSize())
                .Replace("_250", "_" + ImageSize())
                .Replace("_100", "_" + ImageSize())
                .Replace("_75sq", "_" + ImageSize())
                .ToString();
        }

        /// <returns>
        ///     Return the url without the size and type suffix (e.g.
        ///     https://68.media.tumblr.com/51a99943f4aa7068b6fd9a6b36e4961b/tumblr_mnj6m9Huml1qat3lvo1).
        /// </returns>
        protected override string GetCoreImageUrl(string url)
        {
            // return url.Split('_')[0] + "_" + url.Split('_')[1];
            return url;
        }

        protected override async Task<bool> DownloadBinaryPost(TumblrPost downloadItem)
        {
            if (!(downloadItem is PhotoPost))
                return await base.DownloadBinaryPost(downloadItem);
            string url = Url(downloadItem);

            if (blog.ForceSize)
            {
                url = ResizeTumblrImageUrl(url);
            }

            foreach (string host in shellService.Settings.TumblrHosts)
            {
                url = BuildRawImageUrl(url, host);
                if (await base.DownloadBinaryPost(new PhotoPost(url, downloadItem.Id, downloadItem.Date)))
                    return true;
            }
            return await base.DownloadBinaryPost(downloadItem);
        }

        /// <summary>
        /// Builds a tumblr raw image url from any sized tumblr image url if the ImageSize is set to "raw".
        /// </summary>
        /// <param name="url">The url detected from the crawler.</param>
        /// <param name="host">Hostname to insert in the original url.</param>
        /// <returns></returns>
        public string BuildRawImageUrl(string url, string host)
        {
            if (shellService.Settings.ImageSize == "raw")
            {
                string path = new Uri(url).LocalPath.TrimStart('/');
                var imageDimension = new Regex("_\\d+");
                path = imageDimension.Replace(path, "_raw");
                return "https://" + host + "/" + path;
            }
            return url;
        }
    }
}
