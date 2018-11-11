using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public class UguuParser : IUguuParser
    {
        public Regex GetUguuUrlRegex()
        {
            return new Regex("(http[A-Za-z0-9_/:.]*a.uguu.se/(.*))");
        }

        public string GetUguuId(string url)
        {
            return GetUguuUrlRegex().Match(url).Groups[2].Value;
        }

        public string CreateUguuUrl(string uguuId, string detectedUrl, UguuTypes uguuType)
        {
            string url;
            switch (uguuType)
            {
                case UguuTypes.Mp4:
                    url = @"https://a.uguu.se/" + uguuId + ".mp4";
                    break;
                case UguuTypes.Webm:
                    url = @"https://a.uguu.se/" + uguuId + ".webm";
                    break;
                case UguuTypes.Any:
                    url = detectedUrl;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return url;
        }

        public IEnumerable<string> SearchForUguuUrl(string searchableText, UguuTypes UguuType)
        {
            Regex regex = GetUguuUrlRegex();
            foreach (Match match in regex.Matches(searchableText))
            {
                string temp = match.Groups[0].ToString();
                string id = match.Groups[2].Value;
                string url = temp.Split('\"').First();

                yield return CreateUguuUrl(id, url, UguuType);
            }
        }
    }
}
