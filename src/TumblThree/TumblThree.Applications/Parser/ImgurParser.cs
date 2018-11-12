using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;

namespace TumblThree.Applications.Parser
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

        public string GetImgurId(string url) => GetImgurImageRegex().Match(url).Groups[2].Value;

        public Regex GetImgurImageRegex() => new Regex("(http[A-Za-z0-9_/:.]*i.imgur.com/([A-Za-z0-9_]*)(.jpg|.png|.gif|.gifv))");

        public Regex GetImgurAlbumRegex() => new Regex("(http[A-Za-z0-9_/:.]*imgur.com/[aA]/([A-Za-z0-9_]*))");

        public Regex GetImgurAlbumHashRegex() => new Regex("\"hash\":\"([a-zA-Z0-9]*)\"");

        public Regex GetImgurAlbumExtRegex() => new Regex("\"ext\":\"([.a-zA-Z0-9]*)\"");

        public virtual async Task<string> RequestImgurAlbumSite(string imgurAlbumUrl)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                HttpWebRequest request = webRequestFactory.CreateGetReqeust(imgurAlbumUrl);
                requestRegistration = ct.Register(() => request.Abort());
                return await webRequestFactory.ReadReqestToEnd(request);
            }
            finally
            {
                requestRegistration.Dispose();
            }
        }

        public IEnumerable<string> SearchForImgurUrl(string searchableText)
        {
            Regex regex = GetImgurImageRegex();
            foreach (Match match in regex.Matches(searchableText))
            {             
                yield return match.Groups[1].Value;
            }
        }

        public async Task<IEnumerable<string>> SearchForImgurUrlFromAlbumAsync(string searchableText)
        {
            var imageUrls = new List<string>();

            // album urls
            Regex regex = GetImgurAlbumRegex();
            foreach (Match match in regex.Matches(searchableText))
            {
                string albumUrl = match.Groups[1].Value;
                string imgurId = match.Groups[2].Value;
                string album = await RequestImgurAlbumSite(albumUrl);

                Regex hashRegex = GetImgurAlbumHashRegex();
                MatchCollection hashMatches = hashRegex.Matches(album);
                List<string> hashes = hashMatches.Cast<Match>().Select(hashMatch => hashMatch.Groups[1].Value).ToList();

                Regex extRegex = GetImgurAlbumExtRegex();
                MatchCollection extMatches = extRegex.Matches(album);
                List<string> exts = extMatches.Cast<Match>().Select(extMatch => extMatch.Groups[1].Value).ToList();

                imageUrls.AddRange(hashes.Zip(exts, (hash, ext) => "https://i.imgur.com/" + hash + ext));
            }

            return imageUrls;
        }
    }
}
