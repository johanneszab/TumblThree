using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class PhotoMetaPost : TumblrPost
    {
        public PhotoMetaPost(string url, string id, string date) : base(url, id, date)
        {
            PostType = PostType.Text;
            DbType = "DownloadedPhotoMetas";
            TextFileLocation = Resources.FileNameMetaPhoto;
        }

        public PhotoMetaPost(string url, string id) : this(url, id, string.Empty)
        {
        }
    }
}
