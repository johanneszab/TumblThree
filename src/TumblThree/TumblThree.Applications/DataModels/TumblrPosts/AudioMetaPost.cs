using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class AudioMetaPost : TumblrPost
    {
        public AudioMetaPost(string url, string id, string date,UrlType utype=UrlType.none) : base(url, id, date,utype)
        {
            PostType = PostType.Text;
            DbType = "DownloadedAudioMetas";
            TextFileLocation = Resources.FileNameMetaAudio;
        }

        public AudioMetaPost(string url, string id) : this(url, id, string.Empty)
        {
        }
    }
}
