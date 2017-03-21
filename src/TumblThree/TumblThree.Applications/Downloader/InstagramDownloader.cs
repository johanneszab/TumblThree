using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Downloader
{
    [ExportMetadata("BlogType", BlogTypes.instagram)]
    class InstagramDownloader
    {
    }
}
