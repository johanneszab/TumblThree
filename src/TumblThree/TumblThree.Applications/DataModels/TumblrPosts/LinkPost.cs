using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class LinkPost : TumblrPost
    {
        public LinkPost(string url, string id, string date) : base(url, id, date)
        {
            PostType = PostType.Text;
            DbType = "DownloadedPhotos";
            TextFileLocation = Resources.FileNamePhotos;
        }

        public LinkPost(string url, string id) : base(url, id, string.Empty)
        {
        }
    }
}
