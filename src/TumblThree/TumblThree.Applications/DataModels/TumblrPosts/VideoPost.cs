using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class VideoPost : TumblrPost
    {
        public VideoPost(string url, string id, string date,UrlType utype=UrlType.none) : base(url, id, date,utype)
        {
            PostType = PostType.Binary;
            DbType = "DownloadedVideos";
            TextFileLocation = Resources.FileNameVideos;
        }

        public VideoPost(string url, string id) : this(url, id, string.Empty)
        {
        }
    }
}
