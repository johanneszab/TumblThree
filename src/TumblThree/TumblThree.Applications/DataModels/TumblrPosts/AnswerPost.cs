using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class AnswerPost : TumblrPost
    {
        public AnswerPost(string url, string id, string date) : base(url, id, date)
        {
            PostType = PostType.Text;
            DbType = "DownloadedAnswers";
            TextFileLocation = Resources.FileNameAnswers;
        }

        public AnswerPost(string url, string id) : base(url, id, string.Empty)
        {
        }
    }
}
