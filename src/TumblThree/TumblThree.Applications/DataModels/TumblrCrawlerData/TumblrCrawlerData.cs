namespace TumblThree.Applications.DataModels.TumblrCrawlerData
{
    public class TumblrCrawlerData<T> : ITumblrCrawlerData
    {
        public T Data { get; protected set; }

        public string Filename { get; protected set; }

        public TumblrCrawlerData(string filename, T data)
        {
            this.Filename = filename;
            this.Data = data;
        }
    }
}
