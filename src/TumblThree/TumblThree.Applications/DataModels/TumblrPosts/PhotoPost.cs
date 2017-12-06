using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class PhotoPost : TumblrPost
    {
        public PhotoPost(string url, string id, string date) : base(url, id, date)
        {
            PostType = PostType.Binary;
            DbType = "DownloadedPhotos";
            TextFileLocation = Resources.FileNamePhotos;
        }

        public PhotoPost(string url, string id) : this(url, id, string.Empty)
        {
        }
    }
}
