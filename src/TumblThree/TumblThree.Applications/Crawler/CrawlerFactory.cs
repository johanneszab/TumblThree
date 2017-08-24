using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawlerFactory))]
    public class CrawlerFactory : ICrawlerFactory
    {
        private readonly AppSettings settings;

        [ImportingConstructor]
        internal CrawlerFactory(ShellService shellService)
        {
            this.settings = shellService.Settings;
        }

        [ImportMany(typeof(ICrawler))]
        private IEnumerable<Lazy<ICrawler, IBlogTypeMetaData>> DownloaderFactoryLazy { get; set; }

        public ICrawler GetCrawler(BlogTypes blogtype)
        {
            Lazy<ICrawler, IBlogTypeMetaData> downloaderInstance =
                DownloaderFactoryLazy.FirstOrDefault(list => list.Metadata.BlogType == blogtype);

            if (downloaderInstance != null)
            {
                return downloaderInstance.Value;
            }
            throw new ArgumentException("Website is not supported!", "blogType");
        }

        public ICrawler GetCrawler(BlogTypes blogtype, CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress, IShellService shellService, ICrawlerService crawlerService, IBlog blog)
        {
            BlockingCollection<TumblrPost> producerConsumerCollection = GetProducerConsumerCollection();
            switch (blogtype)
            {
                case BlogTypes.tumblr:
                    return new TumblrBlogCrawler(shellService, ct, pt, progress, crawlerService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, blog, producerConsumerCollection), producerConsumerCollection, blog, LoadFiles(blog));
                case BlogTypes.tmblrpriv:
                    return new TumblrPrivateCrawler(shellService, ct, pt, progress, crawlerService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, blog, producerConsumerCollection), producerConsumerCollection, blog, LoadFiles(blog));
                case BlogTypes.tlb:
                    return new TumblrLikedByCrawler(shellService, ct, pt, progress, crawlerService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, blog, producerConsumerCollection), producerConsumerCollection, blog, LoadFiles(blog));
                case BlogTypes.tumblrsearch:
                    return new TumblrSearchCrawler(shellService, ct, pt, progress, crawlerService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, blog, producerConsumerCollection), producerConsumerCollection, blog, LoadFiles(blog));
                case BlogTypes.tumblrtagsearch:
                    return new TumblrTagSearchCrawler(shellService, ct, pt, progress, crawlerService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, blog, producerConsumerCollection), producerConsumerCollection, blog, LoadFiles(blog));
                default:
                    throw new ArgumentException("Website is not supported!", "blogType");
            }
        }

        private IFiles LoadFiles(IBlog blog)
        {
            return new Files().Load(blog.ChildId);
        }

        private FileDownloader GetFileDownloader(CancellationToken ct)
        {
            return new FileDownloader(settings, ct);
        }

        private TumblrDownloader GetTumblrDownloader(CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress, IShellService shellService, ICrawlerService crawlerService, IBlog blog, BlockingCollection<TumblrPost> producerConsumerCollection)
        {
            return new TumblrDownloader(shellService, ct, pt, progress, new PostCounter(blog), producerConsumerCollection, GetFileDownloader(ct), crawlerService, blog, LoadFiles(blog));
        }

        private BlockingCollection<TumblrPost> GetProducerConsumerCollection()
        {
            return new BlockingCollection<TumblrPost>();
        }
    }
}
