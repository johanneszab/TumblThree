using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrCrawlerData;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Downloader
{
    public class TumblrXmlDownloader : ICrawlerDataDownloader
    {
        protected readonly IBlog blog;
        protected readonly ICrawlerService crawlerService;
        protected readonly IPostQueue<TumblrCrawlerData<XDocument>> xmlQueue;
        protected readonly IShellService shellService;
        protected readonly CancellationToken ct;
        protected readonly PauseToken pt;

        public TumblrXmlDownloader(IShellService shellService, CancellationToken ct, PauseToken pt, IPostQueue<TumblrCrawlerData<XDocument>> xmlQueue, ICrawlerService crawlerService, IBlog blog)
        {
            this.shellService = shellService;
            this.crawlerService = crawlerService;
            this.blog = blog;
            this.ct = ct;
            this.pt = pt;
            this.xmlQueue = xmlQueue;
        }

        public virtual async Task DownloadCrawlerDataAsync()
        {
            var trackedTasks = new List<Task>();
            blog.CreateDataFolder();

            foreach (TumblrCrawlerData<XDocument> downloadItem in xmlQueue.GetConsumingEnumerable())
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }
                if (pt.IsPaused)
                {
                    pt.WaitWhilePausedWithResponseAsyc().Wait();
                }

                trackedTasks.Add(new Func<Task>(async () =>
                {
                    try { await DownloadTextPost(downloadItem); }
                    catch { }
                })());
            }
            try { await Task.WhenAll(trackedTasks); }
            catch { }
        }


        private async Task DownloadTextPost(TumblrCrawlerData<XDocument> crawlerData)
        {
            string blogDownloadLocation = blog.DownloadLocation();
            string fileLocation = FileLocation(blogDownloadLocation, crawlerData.Filename);
            await AppendToTextFile(fileLocation, crawlerData.Data);
        }

        private async Task AppendToTextFile(string fileLocation, XContainer data)
        {
            try
            {
                using (var sw = new StreamWriter(fileLocation, true))
                {
                    await sw.WriteAsync(PrettyXml(data));
                }
            }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x27 || (ex.HResult & 0xFFFF) == 0x70)
            {
                Logger.Error("TumblrXmlDownloader:AppendToTextFile: {0}", ex);
                shellService.ShowError(ex, Resources.DiskFull);
                crawlerService.StopCommand.Execute(null);
            }
            catch
            {
            }
        }

        static string PrettyXml(XContainer xml)
        {
            var stringBuilder = new StringBuilder();

            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = true;
            settings.NewLineOnAttributes = true;

            using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
            {
                xml.WriteTo(xmlWriter);
            }

            return stringBuilder.ToString();
        }

        private static string FileLocation(string blogDownloadLocation, string fileName)
        {
            return Path.Combine(blogDownloadLocation, fileName);
        }
    }
}
