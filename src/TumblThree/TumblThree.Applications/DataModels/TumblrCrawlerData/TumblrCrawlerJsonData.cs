using TumblThree.Applications.DataModels.TumblrSvcJson;

namespace TumblThree.Applications.DataModels.TumblrCrawlerData
{
    public class TumblrCrawlerJsonData : ITumblrCrawlerData
    {
        public Post Data { get; protected set; }

        public string Filename { get; protected set; }

        public TumblrCrawlerJsonData(string filename, Post data)
        {
            this.Filename = filename;
            this.Data = data;
        }
    }
}
