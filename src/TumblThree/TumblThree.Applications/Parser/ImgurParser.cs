using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TumblThree.Applications.Extensions;
using TumblThree.Applications.Properties;

namespace TumblThree.Applications.Crawler
{
    public class ImgurParser : IImgurParser
    {
        private readonly AppSettings settings;
        private readonly IWebRequestFactory webRequestFactory;
        private readonly CancellationToken ct;

        public ImgurParser(AppSettings settings, IWebRequestFactory webRequestFactory, CancellationToken ct)
        {
            this.settings = settings;
            this.webRequestFactory = webRequestFactory;
            this.ct = ct;
        }

        public Regex GetImgurImageRegex()
        {
            return new Regex("(http[A-Za-z0-9_/:.]*i.imgur.com/([A-Za-z0-9_]*)(.jpg|.png|.gif|.gifv))");
        }

        public Regex GetImgurAlbumRegex()
        {
            return new Regex("(http[A-Za-z0-9_/:.]*imgur.com/[aA]/([A-Za-z0-9_]*))");
        }

        public Regex GetImgurAlbumHashRegex()
        {
            return new Regex("\"hash\":\"([a-zA-Z0-9]*)\"");
        }

        public Regex GetImgurAlbumExtRegex()
        {
            return new Regex("\"ext\":\"([.a-zA-Z0-9]*)\"");
        }

        public virtual async Task<string> RequestImgurAlbumSite(string imgurAlbumUrl)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                HttpWebRequest request = webRequestFactory.CreateGetReqeust(imgurAlbumUrl);
                requestRegistration = ct.Register(() => request.Abort());
                return await webRequestFactory.ReadReqestToEnd(request).TimeoutAfter(settings.TimeOut);
            }
            finally
            {
                requestRegistration.Dispose();
            }
        }
    }
}
