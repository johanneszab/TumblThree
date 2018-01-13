using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class ConversationPost : TumblrPost
    {
        public ConversationPost(string url, string id, string date,UrlType utype=UrlType.none) : base(url, id, date,utype)
        {
            PostType = PostType.Text;
            DbType = "DownloadedConversations";
            TextFileLocation = Resources.FileNameConversations;
        }

        public ConversationPost(string url, string id) : this(url, id, string.Empty)
        {
        }
    }
}
