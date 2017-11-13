using System;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
    public class WebmshareParser : IWebmshareParser
    {
        public string CreateWebmshareUrl(string webshareId, WebmshareTypes webmshareType)
        {
            string url;
            switch (webmshareType)
            {
                case WebmshareTypes.Mp4:
                    url = @"https://s1.webmshare.com/f/" + webshareId + ".mp4";
                    break;
                case WebmshareTypes.Webm:
                    url = @"https://s1.webmshare.com/" + webshareId + ".webm";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return url;
        }
    }
}