using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public class CatBoxParser : ICatBoxParser
    {
        public Regex GetCatBoxUrlRegex()
        {
            return new Regex("(http[A-Za-z0-9_/:.]*files.catbox.moe/(.*))");
        }

        public string GetCatBoxId(string url)
        {
            return GetCatBoxUrlRegex().Match(url).Groups[2].Value;
        }

        public string CreateCatBoxUrl(string id, string detectedUrl, CatBoxTypes type)
        {
            string url;
            switch (type)
            {
                case CatBoxTypes.Mp4:
                    url = @"https://files.catbox.moe/" + id + ".mp4";
                    break;
                case CatBoxTypes.Webm:
                    url = @"https://files.catbox.moe/" + id + ".webm";
                    break;
                case CatBoxTypes.Any:
                    url = detectedUrl;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return url;
        }

        public IEnumerable<string> SearchForCatBoxUrl(string searchableText, CatBoxTypes catBoxType)
        {
            Regex regex = GetCatBoxUrlRegex();
            foreach (Match match in regex.Matches(searchableText))
            {
                string temp = match.Groups[0].ToString();
                string id = match.Groups[2].Value;
                string url = temp.Split('\"').First();

                yield return CreateCatBoxUrl(id, url, catBoxType);
            }
        }
    }
}
