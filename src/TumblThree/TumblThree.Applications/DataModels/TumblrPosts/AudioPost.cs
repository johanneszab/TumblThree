using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class AudioPost : TumblrPost
    {
        public AudioPost(string url, string id, string date,UrlType utype=UrlType.none) : base(url, id, date,utype)
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
