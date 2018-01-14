using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class AudioPost : TumblrPost
    {
        public AudioPost(string url, string id, string date) : base(url, id, date)
        {
            PostType = PostType.Binary;
            DbType = "DownloadedAudios";
            TextFileLocation = Resources.FileNameAudios;
        }

        public AudioPost(string url, string id) : this(url, id, string.Empty)
        {
        }
    }
}
