using System.ComponentModel.Composition;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Downloader
{
    [ExportMetadata("BlogType", BlogTypes.instagram)]
    class InstagramDownloader
    {
    }
}
