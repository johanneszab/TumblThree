using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrCrawlerData;
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
        private readonly ISharedCookieService cookieService;

        [ImportingConstructor]
        internal CrawlerFactory(ShellService shellService, ISharedCookieService cookieService)
        {
            this.settings = shellService.Settings;
            this.cookieService = cookieService;
        }

        [ImportMany(typeof(ICrawler))]
        private IEnumerable<Lazy<ICrawler, ICrawlerData>> DownloaderFactoryLazy { get; set; }

        public ICrawler GetCrawler(IBlog blog)
        {
            Lazy<ICrawler, ICrawlerData> downloader =
                DownloaderFactoryLazy.FirstOrDefault(list => list.Metadata.BlogType.GetType() == blog.GetType());

            if (downloader != null)
            {
                return downloader.Value;
            }
            throw new ArgumentException("Website is not supported!", "blogType");
        }

        public ICrawler GetCrawler(IBlog blog, CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress, IShellService shellService, ICrawlerService crawlerService, IManagerService managerService)
        {
            BlockingCollection<TumblrPost> producerConsumerCollection = GetProducerConsumerCollection();
            IFiles files = LoadFiles(blog, managerService);
            IWebRequestFactory webRequestFactory = GetWebRequestFactory();
            IImgurParser imgurParser = GetImgurParser(webRequestFactory, ct);
            IGfycatParser gfycatParser = GetGfycatParser(webRequestFactory, ct);
            switch (blog.BlogType)
            {
                case BlogTypes.tumblr:
                    BlockingCollection<TumblrCrawlerXmlData> xmlQueue = GetXmlQueue();
                    return new TumblrBlogCrawler(shellService, ct, pt, progress, crawlerService, webRequestFactory, cookieService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, managerService, blog, files, producerConsumerCollection), GetTumblrXmlDownloader(shellService, ct, pt, xmlQueue, crawlerService, blog), imgurParser, gfycatParser, GetWebmshareParser(), producerConsumerCollection, xmlQueue, blog);
                case BlogTypes.tmblrpriv:
                    BlockingCollection<TumblrCrawlerJsonData> jsonQueue = GetJsonQueue();
                    return new TumblrHiddenCrawler(shellService, ct, pt, progress, crawlerService, webRequestFactory ,cookieService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, managerService, blog, files, producerConsumerCollection), GetTumblrJsonDownloader(shellService, ct, pt, jsonQueue, crawlerService, blog), imgurParser, gfycatParser, GetWebmshareParser(), producerConsumerCollection, jsonQueue, blog);
                case BlogTypes.tlb:
                    return new TumblrLikedByCrawler(shellService, ct, pt, progress, crawlerService, webRequestFactory, cookieService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, managerService, blog, files, producerConsumerCollection), producerConsumerCollection, blog);
                case BlogTypes.tumblrsearch:
                    return new TumblrSearchCrawler(shellService, ct, pt, progress, crawlerService, webRequestFactory, cookieService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, managerService, blog, files, producerConsumerCollection), producerConsumerCollection, blog);
                case BlogTypes.tumblrtagsearch:
                    return new TumblrTagSearchCrawler(shellService, ct, pt, progress, crawlerService, webRequestFactory, cookieService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, managerService, blog, files, producerConsumerCollection), producerConsumerCollection, blog);
                default:
                    throw new ArgumentException("Website is not supported!", "blogType");
            }
        }

        private IFiles LoadFiles(IBlog blog, IManagerService managerService)
        {
            if (settings.LoadAllDatabases)
            {
                return managerService.Databases.Where(file => file.Name.Equals(blog.Name) && file.BlogType.Equals(blog.BlogType)).FirstOrDefault();
            }
            return new Files().Load(blog.ChildId);
        }

        private IWebRequestFactory GetWebRequestFactory()
        {
            return new WebRequestFactory(settings);
        }
        private IImgurParser GetImgurParser(IWebRequestFactory webRequestFactory, CancellationToken ct)
        {
            return new ImgurParser(settings, webRequestFactory, ct);
        }

        private IGfycatParser GetGfycatParser(IWebRequestFactory webRequestFactory, CancellationToken ct)
        {
            return new GfycatParser(settings, webRequestFactory, ct);
        }

        private IWebmshareParser GetWebmshareParser()
        {
            return new WebmshareParser();
        }

        private FileDownloader GetFileDownloader(CancellationToken ct)
        {
            return new FileDownloader(settings, ct, cookieService);
        }

        private static IBlogService GetBlogService(IBlog blog, IFiles files)
        {
            return new BlogService(blog, files);
        }

        private TumblrDownloader GetTumblrDownloader(CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress, IShellService shellService, ICrawlerService crawlerService, IManagerService managerService, IBlog blog, IFiles files, BlockingCollection<TumblrPost> producerConsumerCollection)
        {
            return new TumblrDownloader(shellService, managerService, ct, pt, progress, producerConsumerCollection, GetFileDownloader(ct), crawlerService, blog, files);
        }

        private TumblrXmlDownloader GetTumblrXmlDownloader(IShellService shellService, CancellationToken ct, PauseToken pt, BlockingCollection<TumblrCrawlerXmlData> xmlQueue, ICrawlerService crawlerService, IBlog blog)
        {
            return new TumblrXmlDownloader(shellService, ct, pt, xmlQueue, crawlerService, blog);
        }

        private TumblrJsonDownloader GetTumblrJsonDownloader(IShellService shellService, CancellationToken ct, PauseToken pt, BlockingCollection<TumblrCrawlerJsonData> jsonQueue, ICrawlerService crawlerService, IBlog blog)
        {
            return new TumblrJsonDownloader(shellService, ct, pt, jsonQueue, crawlerService, blog);
        }

        private BlockingCollection<TumblrPost> GetProducerConsumerCollection()
        {
            return new BlockingCollection<TumblrPost>();
        }

        private BlockingCollection<TumblrCrawlerXmlData> GetXmlQueue()
        {
            return new BlockingCollection<TumblrCrawlerXmlData>();
        }

        private BlockingCollection<TumblrCrawlerJsonData> GetJsonQueue()
        {
            return new BlockingCollection<TumblrCrawlerJsonData>();
        }
    }
}
