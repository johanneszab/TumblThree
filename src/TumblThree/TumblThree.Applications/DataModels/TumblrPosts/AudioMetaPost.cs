using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class AudioMetaPost : TumblrPost
    {
        public AudioMetaPost(string url, string id, string date) : base(url, id, date)
        {
            PostType = PostType.Text;
            DbType = "DownloadedAudioMetas";
            TextFileLocation = Resources.FileNameMetaAudio;
        }

        public AudioMetaPost(string url, string id) : base(url, id, string.Empty)
        {
        }
    }
}
