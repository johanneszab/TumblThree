using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TumblThree.Applications.Parser
{
    public class TumblrParser : ITumblrParser
    {
        public Regex GetTumblrPhotoUrlRegex()
        {
            return new Regex("\"(http[A-Za-z0-9_/:.]*media.tumblr.com[A-Za-z0-9_/:.]*(jpg|png|gif))\"");
        }

        public Regex GetGenericPhotoUrlRegex()
        {
            return new Regex("\"(https?://(?:[a-z0-9\\-]+\\.)+[a-z]{2,6}(?:/[^/#?]+)+\\.(?:jpg|png|gif))\"");
        }

        public Regex GetTumblrVeVideoUrlRegex()
        {
            return new Regex("\"(https?://ve.media.tumblr.com/(tumblr_[\\w]*))");
        }

        public Regex GetTumblrVttVideoUrlRegex()
        {
            return new Regex("\"(https?://vtt.tumblr.com/(tumblr_[\\w]*))");
        }

        public Regex GetTumblrInlineVideoUrlRegex()
        {
            return new Regex("\"(http[A-Za-z0-9_/:.]*.com/video_file/[A-Za-z0-9_/:.]*)\"");
        }

        public Regex GetGenericVideoUrlRegex()
        {
            return new Regex("\"(https?://(?:[a-z0-9\\-]+\\.)+[a-z]{2,6}(?:/[^/#?]+)+\\.(?:mp4|mkv|gifv))\"");
        }

        public IEnumerable<string> SearchForTumblrPhotoUrl(string searchableText)
        {
            Regex regex = GetTumblrPhotoUrlRegex();
            foreach (Match match in regex.Matches(searchableText))
            {
                string imageUrl = match.Groups[1].Value;
                if (imageUrl.Contains("avatar") || imageUrl.Contains("previews"))
                    continue;

                yield return imageUrl;
            }
        }

        public IEnumerable<string> SearchForTumblrVideoUrl(string searchableText)
        {
            Regex regex = GetTumblrInlineVideoUrlRegex();
            foreach (Match match in regex.Matches(searchableText))
            {
                string videoUrl = match.Groups[2].Value;

                yield return videoUrl;
            }
        }

        public IEnumerable<string> SearchForGenericPhotoUrl(string searchableText)
        {
            Regex regex = GetGenericPhotoUrlRegex();
            foreach (Match match in regex.Matches(searchableText))
            {
                string imageUrl = match.Groups[1].Value;
                if (imageUrl.Contains("avatar") || imageUrl.Contains("previews"))
                    continue;

                yield return imageUrl;
            }
        }

        public IEnumerable<string> SearchForGenericVideoUrl(string searchableText)
        {
            Regex regex = GetGenericVideoUrlRegex();
            foreach (Match match in regex.Matches(searchableText))
            {
                string videoUrl = match.Groups[1].Value;

                yield return videoUrl;
            }
        }
    }
}
