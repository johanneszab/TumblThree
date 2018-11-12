namespace TumblThree.Applications.Services
{
    public static class CrawlerServiceExtensions
    {
        public static void Crawl(this ICrawlerService crawlerService)
        {
            if (crawlerService.IsCrawl)
                crawlerService.CrawlCommand.Execute(null);
        }
    }
}
