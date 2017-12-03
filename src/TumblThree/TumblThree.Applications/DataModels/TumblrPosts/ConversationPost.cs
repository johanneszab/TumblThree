using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class ConversationPost : TumblrPost
    {
        public ConversationPost(string url, string id, string date) : base(url, id, date)
        {
            PostType = PostType.Text;
            DbType = "DownloadedConversations";
            TextFileLocation = Resources.FileNameConversations;
        }

        public ConversationPost(string url, string id) : base(url, id, string.Empty)
        {
        }
    }
}
