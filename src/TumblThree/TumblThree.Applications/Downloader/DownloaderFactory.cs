using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Downloader
{
    [Export(typeof(IDownloaderFactory))]
    public class DownloaderFactory : IDownloaderFactory
    {
        private readonly AppSettings settings;

        [ImportingConstructor]
        internal DownloaderFactory(ShellService shellService)
        {
            this.settings = shellService.Settings;
        }

        [ImportMany(typeof(IDownloader))]
        private IEnumerable<Lazy<IDownloader, IBlogTypeMetaData>> DownloaderFactoryLazy { get; set; }

        public IDownloader GetDownloader(BlogTypes blogtype)
        {
            Lazy<IDownloader, IBlogTypeMetaData> downloaderInstance =
                DownloaderFactoryLazy.FirstOrDefault(list => list.Metadata.BlogType == blogtype);

            if (downloaderInstance != null)
            {
                return downloaderInstance.Value;
            }
            throw new ArgumentException("Website is not supported!", "blogType");
        }

        public IDownloader GetDownloader(BlogTypes blogtype, CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress, IShellService shellService, ICrawlerService crawlerService, IBlog blog)
        {
            switch (blogtype)
            {
                case BlogTypes.tumblr:
                    return new TumblrBlogDownloader(shellService, ct, pt, progress, new PostCounter(blog), GetFileDownloader(ct), crawlerService, blog, LoadFiles(blog));
                case BlogTypes.tmblrpriv:
                    return new TumblrPrivateDownloader(shellService, ct, pt, progress, new PostCounter(blog), GetFileDownloader(ct), crawlerService, blog, LoadFiles(blog));
                case BlogTypes.tlb:
                    return new TumblrLikedByDownloader(shellService, ct, pt, progress, new PostCounter(blog), GetFileDownloader(ct), crawlerService, blog, LoadFiles(blog));
                case BlogTypes.tumblrsearch:
                    return new TumblrSearchDownloader(shellService, ct, pt, progress, new PostCounter(blog), GetFileDownloader(ct), crawlerService, blog, LoadFiles(blog));
                case BlogTypes.tumblrtagsearch:
                    return new TumblrTagSearchDownloader(shellService, ct, pt, progress, new PostCounter(blog), GetFileDownloader(ct), crawlerService, blog, LoadFiles(blog));
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
    }
}
