using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class AnswerPost : TumblrPost
    {
        public AnswerPost(string url, string id, string date, UrlType utype=UrlType.none) : base(url, id, date,utype)
        {
            PostType = PostType.Text;
            DbType = "DownloadedAnswers";
            TextFileLocation = Resources.FileNameAnswers;
        }

        public AnswerPost(string url, string id) : this(url, id, string.Empty)
        {
        }
    }
}
