using System.Text.RegularExpressions;

namespace TumblThree.Applications.Crawler
{
    public interface ITumblrParser
    {
        Regex GetTumblrPhotoUrlRegex();

        Regex GetTumblrVideoUrlRegex();
    }
}