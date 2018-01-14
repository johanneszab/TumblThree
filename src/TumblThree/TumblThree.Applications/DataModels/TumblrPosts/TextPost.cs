using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class TextPost : TumblrPost
    {
        public TextPost(string url, string id, string date) : base(url, id, date)
        {
            PostType = PostType.Text;
            DbType = "DownloadedTexts";
            TextFileLocation = Resources.FileNameTexts;
        }

        public TextPost(string url, string id) : this(url, id, string.Empty)
        {
        }
    }
}
