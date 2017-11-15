using System.Text.RegularExpressions;

namespace TumblThree.Applications.Crawler
{
    public interface IImgurParser
    {
        Regex GetImgurUrlRegex();
    }
}
