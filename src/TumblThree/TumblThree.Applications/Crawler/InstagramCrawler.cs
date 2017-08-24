using System.ComponentModel.Composition;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
    [ExportMetadata("BlogType", BlogTypes.instagram)]
    class InstagramCrawler
    {
    }
}
