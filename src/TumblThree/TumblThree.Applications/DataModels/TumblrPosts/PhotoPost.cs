using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class PhotoPost : TumblrPost
    {
        public PhotoPost(string url, string id, string date,UrlType utype=UrlType.none) : base(url, id, date,utype)
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
