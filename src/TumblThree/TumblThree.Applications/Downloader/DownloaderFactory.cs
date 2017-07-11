using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using TumblThree.Applications.Services;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Downloader
{
    [Export(typeof(IDownloaderFactory))]
    public class DownloaderFactory : IDownloaderFactory
    {
        [ImportingConstructor]
        public DownloaderFactory()
        {
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

        public IDownloader GetDownloader(BlogTypes blogtype, IShellService shellService, ICrawlerService crawlerService, IBlog blog)
        {
            switch (blogtype)
            {
                case BlogTypes.tumblr:
                    return new TumblrDownloader(shellService, crawlerService, blog);
                case BlogTypes.tmblrpriv:
                    return new TumblrPrivateDownloader(shellService, crawlerService, blog);
                case BlogTypes.tlb:
                    return new TumblrLikedByDownloader(shellService, crawlerService, blog);
                case BlogTypes.ts:
                    return new TumblrSearchDownloader(shellService, crawlerService, blog);
                default:
                    throw new ArgumentException("Website is not supported!", "blogType");
            }
        }
    }
}
