using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class ExternalPhotoPost : TumblrPost
    {
        public ExternalPhotoPost(string url, string id, string date) : base(url, id, date)
        {
            PostType = PostType.Binary;
            DbType = "DownloadedPhotos";
            TextFileLocation = Resources.FileNamePhotos;
        }

        public ExternalPhotoPost(string url, string id) : this(url, id, string.Empty)
        {
        }
    }
}
