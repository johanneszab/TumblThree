using System.Text.RegularExpressions;

namespace TumblThree.Applications.Parser
{
    public interface ITumblrParser
    {
        Regex GetTumblrPhotoUrlRegex();

        Regex GetTumblrVideoUrlRegex();
    }
}
