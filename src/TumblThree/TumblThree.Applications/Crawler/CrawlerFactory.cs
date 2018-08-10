using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrCrawlerData;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Parser;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawlerFactory))]
    public class CrawlerFactory : ICrawlerFactory
    {
        private readonly IShellService shellService;
        private readonly ISharedCookieService cookieService;
        private readonly AppSettings settings;

        [ImportingConstructor]
        internal CrawlerFactory(ShellService shellService, ISharedCookieService cookieService)
        {
            this.shellService = shellService;
            this.cookieService = cookieService;
            this.settings = shellService.Settings;
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
            IPostQueue<TumblrPost> postQueue = GetProducerConsumerCollection();
            IFiles files = LoadFiles(blog, managerService);
            IWebRequestFactory webRequestFactory = GetWebRequestFactory();
            IImgurParser imgurParser = GetImgurParser(webRequestFactory, ct);
            IGfycatParser gfycatParser = GetGfycatParser(webRequestFactory, ct);
            switch (blog.BlogType)
            {
                case BlogTypes.tumblr:
                    IPostQueue<TumblrCrawlerData<DataModels.TumblrApiJson.Post>> jsonApiQueue = GetJsonQueue<DataModels.TumblrApiJson.Post>();
                    return new TumblrBlogCrawler(shellService, ct, pt, progress, crawlerService, webRequestFactory, cookieService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, managerService, blog, files, postQueue), postQueue, blog);
                case BlogTypes.tmblrpriv:
                    IPostQueue<TumblrCrawlerData<DataModels.TumblrSvcJson.Post>> jsonSvcQueue = GetJsonQueue<DataModels.TumblrSvcJson.Post>();
                    return new TumblrHiddenCrawler(shellService, ct, pt, progress, crawlerService, webRequestFactory, cookieService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, managerService, blog, files, postQueue), GetTumblrJsonDownloader(shellService, ct, pt, jsonSvcQueue, crawlerService, blog), GetTumblrSvcJsonToTextParser(blog), imgurParser, gfycatParser, GetWebmshareParser(), GetMixtapeParser(), GetUguuParser(), GetSafeMoeParser(), GetLoliSafeParser(), GetCatBoxParser(), postQueue, jsonSvcQueue, blog);
                case BlogTypes.tlb:
                    return new TumblrLikedByCrawler(shellService, ct, pt, progress, crawlerService, webRequestFactory, cookieService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, managerService, blog, files, postQueue), postQueue, blog);
                case BlogTypes.tumblrsearch:
                    return new TumblrSearchCrawler(shellService, ct, pt, progress, crawlerService, webRequestFactory, cookieService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, managerService, blog, files, postQueue), postQueue, blog);
                case BlogTypes.tumblrtagsearch:
                    return new TumblrTagSearchCrawler(shellService, ct, pt, progress, crawlerService, webRequestFactory, cookieService, GetTumblrDownloader(ct, pt, progress, shellService, crawlerService, managerService, blog, files, postQueue), postQueue, blog);
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
            return new WebRequestFactory(shellService, cookieService, settings);
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

        private IMixtapeParser GetMixtapeParser()
        {
            return new MixtapeParser();
        }

        private IUguuParser GetUguuParser()
        {
            return new UguuParser();
        }

        private ISafeMoeParser GetSafeMoeParser()
        {
            return new SafeMoeParser();
        }

        private ILoliSafeParser GetLoliSafeParser()
        {
            return new LoliSafeParser();
        }

        private ICatBoxParser GetCatBoxParser()
        {
            return new CatBoxParser();
        }

        private FileDownloader GetFileDownloader(CancellationToken ct)
        {
            return new FileDownloader(settings, ct, GetWebRequestFactory(), cookieService);
        }

        private static IBlogService GetBlogService(IBlog blog, IFiles files)
        {
            return new BlogService(blog, files);
        }

        private TumblrDownloader GetTumblrDownloader(CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress, IShellService shellService, ICrawlerService crawlerService, IManagerService managerService, IBlog blog, IFiles files, IPostQueue<TumblrPost> postQueue)
        {
            return new TumblrDownloader(shellService, managerService, ct, pt, progress, postQueue, GetFileDownloader(ct), crawlerService, blog, files);
        }

        private TumblrXmlDownloader GetTumblrXmlDownloader(IShellService shellService, CancellationToken ct, PauseToken pt, IPostQueue<TumblrCrawlerData<XDocument>> xmlQueue, ICrawlerService crawlerService, IBlog blog)
        {
            return new TumblrXmlDownloader(shellService, ct, pt, xmlQueue, crawlerService, blog);
        }

        private TumblrJsonDownloader<T> GetTumblrJsonDownloader<T>(IShellService shellService, CancellationToken ct, PauseToken pt, IPostQueue<TumblrCrawlerData<T>> jsonQueue, ICrawlerService crawlerService, IBlog blog)
        {
            return new TumblrJsonDownloader<T>(shellService, ct, pt, jsonQueue, crawlerService, blog);
        }

        private IPostQueue<TumblrPost> GetProducerConsumerCollection()
        {
            return new PostQueue<TumblrPost>(new ConcurrentQueue<TumblrPost>());
        }

        private ITumblrApiXmlToTextParser GetTumblrApiXmlToTextParser()
        {
            return new TumblrApiXmlToTextParser();
        }

        private ITumblrToTextParser<DataModels.TumblrApiJson.Post> GetTumblrApiJsonToTextParser(IBlog blog)
        {
            switch (blog.MetadataFormat)
            {
                case MetadataType.Text:
                    return new TumblrApiJsonToTextParser<DataModels.TumblrApiJson.Post>();
                case MetadataType.Json:
                    return new TumblrApiJsonToJsonParser<DataModels.TumblrApiJson.Post>();
                default:
                    throw new ArgumentException("Website is not supported!", "blogType");
            }
        }

        private ITumblrToTextParser<DataModels.TumblrSvcJson.Post> GetTumblrSvcJsonToTextParser(IBlog blog)
        {
            switch (blog.MetadataFormat)
            {
                case MetadataType.Text:
                    return new TumblrSvcJsonToTextParser<DataModels.TumblrSvcJson.Post>();
                case MetadataType.Json:
                    return new TumblrSvcJsonToJsonParser<DataModels.TumblrSvcJson.Post>();
                default:
                    throw new ArgumentException("Website is not supported!", "blogType");
            }
        }

        private IPostQueue<TumblrCrawlerData<XDocument>> GetApiXmlQueue()
        {
            return new PostQueue<TumblrCrawlerData<XDocument>>(new ConcurrentQueue<TumblrCrawlerData<XDocument>>());
        }

        private IPostQueue<TumblrCrawlerData<T>> GetJsonQueue<T>()
        {
            return new PostQueue<TumblrCrawlerData<T>>(new ConcurrentQueue<TumblrCrawlerData<T>>());
        }
    }
}
