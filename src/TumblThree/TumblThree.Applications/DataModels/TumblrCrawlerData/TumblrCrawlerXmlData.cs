using System.Xml.Linq;

namespace TumblThree.Applications.DataModels.TumblrCrawlerData
{
    public class TumblrCrawlerXmlData : ITumblrCrawlerData
    {
        public XContainer Data { get; protected set; }

        public string Filename { get; protected set; }

        public TumblrCrawlerXmlData(string filename, XContainer data)
        {
            this.Filename = filename;
            this.Data = data;
        }
    }
}
