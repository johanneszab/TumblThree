namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public enum PostType
    {
        Binary,
        Text
    }

    public abstract class TumblrPost
    {
        public PostType PostType { get; protected set; }

        public string Url { get; }

        public string Id { get; }

        public string Date { get; }

        public string DbType { get; protected set; }

        public string TextFileLocation { get; protected set; }

        protected TumblrPost(string url, string id, string date)
        {
            this.Url = url;
            this.Id = id;
            this.Date = date;
        }
    }
}
