using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class QuotePost : TumblrPost
    {
        public QuotePost(string url, string id, string date) : base(url, id, date)
        {
            PostType = PostType.Text;
            DbType = "DownloadedQuotes";
            TextFileLocation = Resources.FileNameQuotes;
        }

        public QuotePost(string url, string id) : base(url, id, string.Empty)
        {
        }
    }
}
