using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public class WebmshareParser : IWebmshareParser
    {
        public Regex GetWebmshareUrlRegex() => new Regex("(http[A-Za-z0-9_/:.]*webmshare.com/([A-Za-z0-9_]*))");

        public string GetWebmshareId(string url) => GetWebmshareUrlRegex().Match(url).Groups[2].Value;

        public string CreateWebmshareUrl(string webshareId, string detectedUrl, WebmshareTypes webmshareType)
        {
            var url = "";
            switch (webmshareType)
            {
                case WebmshareTypes.Mp4:
                    url = @"https://s1.webmshare.com/f/" + webshareId + ".mp4";
                    break;
                case WebmshareTypes.Webm:
                    url = @"https://s1.webmshare.com/" + webshareId + ".webm";
                    break;
                case WebmshareTypes.Any:
                    url = detectedUrl;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return url;
        }

        public IEnumerable<string> SearchForWebmshareUrl(string searchableText, WebmshareTypes webmshareType)
        {
            Regex regex = GetWebmshareUrlRegex();
            foreach (Match match in regex.Matches(searchableText))
            {
                string webmshareId = match.Groups[2].Value;
                string url = match.Groups[0].Value.Split('\"').First();
                yield return CreateWebmshareUrl(webmshareId, url, webmshareType);
            }
        }
    }
}
