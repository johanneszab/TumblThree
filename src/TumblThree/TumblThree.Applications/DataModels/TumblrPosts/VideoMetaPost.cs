using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class VideoMetaPost : TumblrPost
    {
        public VideoMetaPost(string url, string id, string date,UrlType utype=UrlType.none) : base(url, id, date,utype)
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
