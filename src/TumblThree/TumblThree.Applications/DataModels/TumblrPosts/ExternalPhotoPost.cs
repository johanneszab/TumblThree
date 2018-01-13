using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class ExternalPhotoPost : TumblrPost
    {
        public ExternalPhotoPost(string url, string id, string date,UrlType utype=UrlType.none) : base(url, id, date,utype)
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
