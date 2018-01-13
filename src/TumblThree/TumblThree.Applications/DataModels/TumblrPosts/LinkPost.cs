using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class LinkPost : TumblrPost
    {
        public LinkPost(string url, string id, string date,UrlType utype=UrlType.none) : base(url, id, date,utype)
        {
            PostType = PostType.Text;
            DbType = "DownloadedLinks";
            TextFileLocation = Resources.FileNameLinks;
        }

        public LinkPost(string url, string id) : this(url, id, string.Empty)
        {
        }
    }
}
