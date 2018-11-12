using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public class MixtapeParser : IMixtapeParser
    {
        public Regex GetMixtapeUrlRegex() => new Regex("(http[A-Za-z0-9_/:.]*mixtape.moe/(.*))");

        public string GetMixtapeId(string url) => GetMixtapeUrlRegex().Match(url).Groups[2].Value;

        public string CreateMixtapeUrl(string mixtapeId, string detectedUrl, MixtapeTypes mixtapeType)
        {
            string url;
            switch (mixtapeType)
            {
                case MixtapeTypes.Mp4:
                    url = @"https://my.mixtape.moe/" + mixtapeId + ".mp4";
                    break;
                case MixtapeTypes.Webm:
                    url = @"https://my.mixtape.moe/" + mixtapeId + ".webm";
                    break;
                case MixtapeTypes.Any:
                    url = detectedUrl;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return url;
        }

        public IEnumerable<string> SearchForMixtapeUrl(string searchableText, MixtapeTypes mixtapeType)
        {
            Regex regex = GetMixtapeUrlRegex();
            foreach (Match match in regex.Matches(searchableText))
            {
                string temp = match.Groups[0].ToString();
                string id = match.Groups[2].Value;
                string url = temp.Split('\"').First();

                yield return CreateMixtapeUrl(id, url, mixtapeType);
            }
        }
    }
}
