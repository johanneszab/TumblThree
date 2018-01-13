using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class ExternalVideoPost : TumblrPost
    {
        public ExternalVideoPost(string url, string id, string date, UrlType utype=UrlType.none) : base(url, id, date,utype)
        {
            PostType = PostType.Binary;
            DbType = "DownloadedVideos";
            TextFileLocation = Resources.FileNameVideos;
        }

        public ExternalVideoPost(string url, string id) : this(url, id, string.Empty)
        {
        }

	    public ExternalVideoPost(string url) : this(url,string.Empty)
	    {
	    }
    }
}
