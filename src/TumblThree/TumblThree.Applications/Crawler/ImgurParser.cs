using System.Text.RegularExpressions;

namespace TumblThree.Applications.Crawler
{
    public class ImgurParser : IImgurParser
    {
        public Regex GetImgurUrlRegex()
        {
            return new Regex("(http[A-Za-z0-9_/:.]*i.imgur.com/([A-Za-z0-9_])*(.jpg|.png|.gif|.gifv))");
        }
    }
}
