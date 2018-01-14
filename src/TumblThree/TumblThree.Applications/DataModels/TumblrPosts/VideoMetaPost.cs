using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class VideoMetaPost : TumblrPost
    {
        public VideoMetaPost(string url, string id, string date) : base(url, id, date)
        {
            PostType = PostType.Text;
            DbType = "DownloadedVideoMetas";
            TextFileLocation = Resources.FileNameMetaVideo;
        }

        public VideoMetaPost(string url, string id) : this(url, id, string.Empty)
        {
        }
    }
}
