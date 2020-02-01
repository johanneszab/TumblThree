using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TumblThree.Applications.Parser
{
    public class TumblrParser : ITumblrParser
    {
        public Regex GetTumblrPhotoUrlRegex() => new Regex("\"(http[A-Za-z0-9_/:.]*media.tumblr.com[A-Za-z0-9_/:.-]*(jpg|jpeg|tiff|tif|heif|heic|png|gif|webp))\"");

        public Regex GetGenericPhotoUrlRegex() => new Regex("\"(https?://(?:[a-z0-9\\-]+\\.)+[a-z]{2,6}(?:/[^/#?]+)+\\.(?:jpg|jpeg|tiff|tif|heif|heic|png|gif|webp))\"");

        public Regex GetTumblrVVideoUrlRegex() => new Regex("\"(https?://v[A-Za-z0-9_.]*.tumblr.com/(tumblr_[\\w]*))");

        public Regex GetTumblrInlineVideoUrlRegex() => new Regex("\"(http[A-Za-z0-9_/:.]*video_file[\\S]*/(tumblr_[\\w]*))[0-9/]*\"");

        public Regex GetGenericVideoUrlRegex() => new Regex("\"(https?://(?:[a-z0-9\\-]+\\.)+[a-z]{2,6}(?:/[^/#?]+)+\\.(?:mp4|mkv|wmv|mpeg|mpg|avi|gifv|webm))\"");

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

        public bool IsTumblrUrl(string url)
        {
            var regex = new Regex("tumblr_[\\w]*");
            return regex.IsMatch(url);
        }
    }
}
