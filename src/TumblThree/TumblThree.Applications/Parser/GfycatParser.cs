using System;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using TumblThree.Applications.Extensions;
using TumblThree.Applications.Properties;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
    public class GfycatParser : IGfycatParser
    {
        private readonly AppSettings settings;
        private readonly IWebRequestFactory webRequestFactory;
        private readonly CancellationToken ct;

        public GfycatParser(AppSettings settings, IWebRequestFactory webRequestFactory, CancellationToken ct)
        {
            this.settings = settings;
            this.webRequestFactory = webRequestFactory;
            this.ct = ct;
        }

        public Regex GetGfycatUrlRegex()
        {
            return new Regex("(http[A-Za-z0-9_/:.]*gfycat.com/([A-Za-z0-9_]*))");
        }

        public virtual async Task<string> RequestGfycatCajax(string gfyId)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                string url = @"https://gfycat.com/cajax/get/" + gfyId;
                HttpWebRequest request = webRequestFactory.CreateGetXhrReqeust(url);
                requestRegistration = ct.Register(() => request.Abort());
                return await webRequestFactory.ReadReqestToEnd(request).TimeoutAfter(settings.TimeOut);
            }
            finally
            {
                requestRegistration.Dispose();
            }
        }

        public string ParseGfycatCajaxResponse(string result, GfycatTypes gfycatType)
        {
            XmlDictionaryReader jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(result),
                new System.Xml.XmlDictionaryReaderQuotas());
            XElement root = XElement.Load(jsonReader);
            string url;
            switch (gfycatType)
            {
                case GfycatTypes.Gif:
                    url = root.Element("gfyItem").Element("gifUrl").Value;
                    break;
                case GfycatTypes.Max5mbGif:
                    url = root.Element("gfyItem").Element("max5mbGif").Value;
                    break;
                case GfycatTypes.Max2mbGif:
                    url = root.Element("gfyItem").Element("max2mbGif").Value;
                    break;
                case GfycatTypes.Mjpg:
                    url = root.Element("gfyItem").Element("mjpgUrl").Value;
                    break;
                case GfycatTypes.Mp4:
                    url = root.Element("gfyItem").Element("mp4Url").Value;
                    break;
                case GfycatTypes.Poster:
                    url = root.Element("gfyItem").Element("posterUrl").Value;
                    break;
                case GfycatTypes.Webm:
                    url = root.Element("gfyItem").Element("webmUrl").Value;
                    break;
                case GfycatTypes.Webp:
                    url = root.Element("gfyItem").Element("webpUrl").Value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return url;
        }
    }
}