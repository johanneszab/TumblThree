using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrCrawlerData;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Downloader
{
    public class TumblrJsonDownloader<T> : ICrawlerDataDownloader
    {
        protected readonly IBlog blog;
        protected readonly ICrawlerService crawlerService;
        protected readonly IPostQueue<TumblrCrawlerData<T>> jsonQueue;
        protected readonly IShellService shellService;
        protected readonly CancellationToken ct;
        protected readonly PauseToken pt;

        public TumblrJsonDownloader(IShellService shellService, CancellationToken ct, PauseToken pt,
            IPostQueue<TumblrCrawlerData<T>> jsonQueue, ICrawlerService crawlerService, IBlog blog)
        {
            this.shellService = shellService;
            this.crawlerService = crawlerService;
            this.blog = blog;
            this.ct = ct;
            this.pt = pt;
            this.jsonQueue = jsonQueue;
        }

        public virtual async Task DownloadCrawlerDataAsync()
        {
            var trackedTasks = new List<Task>();
            blog.CreateDataFolder();

            foreach (TumblrCrawlerData<T> downloadItem in jsonQueue.GetConsumingEnumerable())
            {
                if (ct.IsCancellationRequested)
                    break;
                
                if (pt.IsPaused)
                    pt.WaitWhilePausedWithResponseAsyc().Wait();
                
                trackedTasks.Add(DownloadPostAsync(downloadItem));
            }

            await Task.WhenAll(trackedTasks);
        }

        private async Task DownloadPostAsync(TumblrCrawlerData<T> downloadItem)
        {
            try
            {
                await DownloadTextPostAsync(downloadItem);
            }
            catch
            {
            }
        }

        private async Task DownloadTextPostAsync(TumblrCrawlerData<T> crawlerData)
        {
            string blogDownloadLocation = blog.DownloadLocation();
            string fileLocation = FileLocation(blogDownloadLocation, crawlerData.Filename);
            await AppendToTextFileAsync(fileLocation, crawlerData.Data);
        }

        private async Task AppendToTextFileAsync(string fileLocation, T data)
        {
            try
            {
                using (var stream = new FileStream(fileLocation, FileMode.Create, FileAccess.Write))
                {
                    using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(
                        stream, Encoding.UTF8, true, true, "  "))
                    {
                        var serializer = new DataContractJsonSerializer(data.GetType());
                        serializer.WriteObject(writer, data);
                        await writer.FlushAsync();
                    }
                }
            }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x27 || (ex.HResult & 0xFFFF) == 0x70)
            {
                Logger.Error("TumblrJsonDownloader:AppendToTextFile: {0}", ex);
                shellService.ShowError(ex, Resources.DiskFull);
                crawlerService.StopCommand.Execute(null);
            }
            catch
            {
            }
        }

        private static string FileLocation(string blogDownloadLocation, string fileName)
        {
            return Path.Combine(blogDownloadLocation, fileName);
        }
    }
}
