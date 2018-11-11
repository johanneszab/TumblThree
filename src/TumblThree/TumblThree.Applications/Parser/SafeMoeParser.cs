using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public class SafeMoeParser : ISafeMoeParser
    {
        public Regex GetSafeMoeUrlRegex()
        {
            return new Regex("(http[A-Za-z0-9_/:.]*a.safe.moe/(.*))");
        }

        public string GetSafeMoeId(string url)
        {
            return GetSafeMoeUrlRegex().Match(url).Groups[2].Value;
        }

        public string CreateSafeMoeUrl(string id, string detectedUrl, SafeMoeTypes type)
        {
            string url;
            switch (type)
            {
                case SafeMoeTypes.Mp4:
                    url = @"https://a.safe.moe/" + id + ".mp4";
                    break;
                case SafeMoeTypes.Webm:
                    url = @"https://a.safe.moe/" + id + ".webm";
                    break;
                case SafeMoeTypes.Any:
                    url = detectedUrl;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return url;
        }

        public IEnumerable<string> SearchForSafeMoeUrl(string searchableText, SafeMoeTypes safeMoeType)
        {
            Regex regex = GetSafeMoeUrlRegex();
            foreach (Match match in regex.Matches(searchableText))
            {
                string temp = match.Groups[0].ToString();
                string id = match.Groups[2].Value;
                string url = temp.Split('\"').First();

                yield return CreateSafeMoeUrl(id, url, safeMoeType);
            }
        }
    }
}
