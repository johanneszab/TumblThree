using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public class LoliSafeParser : ILoliSafeParser
    {
        public Regex GetLoliSafeUrlRegex() => new Regex("(http[A-Za-z0-9_/:.]*(loli.temel.me|3dx.pw)/(.*))");

        public string GetLoliSafeId(string url) => GetLoliSafeUrlRegex().Match(url).Groups[2].Value;

        public string CreateLoliSafeUrl(string id, string detectedUrl, LoliSafeTypes type)
        {
            string url;
            switch (type)
            {
                case LoliSafeTypes.Mp4:
                    url = @"https://3dx.pw/" + id + ".mp4";
                    break;
                case LoliSafeTypes.Webm:
                    url = @"https://3dx.pw/" + id + ".webm";
                    break;
                case LoliSafeTypes.Any:
                    url = detectedUrl;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return url;
        }

        public IEnumerable<string> SearchForLoliSafeUrl(string searchableText, LoliSafeTypes loliSafeType)
        {
            Regex regex = GetLoliSafeUrlRegex();
            foreach (Match match in regex.Matches(searchableText))
            {
                string temp = match.Groups[0].ToString();
                string id = match.Groups[2].Value;
                string url = temp.Split('\"').First();

                yield return CreateLoliSafeUrl(id, url, loliSafeType);
            }
        }
    }
}
