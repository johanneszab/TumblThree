using System.Text.RegularExpressions;

namespace TumblThree.Applications.Crawler
{
    public class TumblrParser : ITumblrParser
    {
        public Regex GetTumblrPhotoUrlRegex()
        {
            return new Regex("\"(http[A-Za-z0-9_/:.]*media.tumblr.com[A-Za-z0-9_/:.]*(jpg|png|gif))\"");
        }

        public Regex GetTumblrVideoUrlRegex()
        {
            return new Regex("\"(http[A-Za-z0-9_/:.]*.com/video_file/[A-Za-z0-9_/:.]*)\"");
        }
    }
}
