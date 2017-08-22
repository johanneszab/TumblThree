using System.Linq;

namespace TumblThree.Domain.Models
{
    public static class Validator
    {
        public static bool IsValidTumblrUrl(string url)
        {
            return url != null && url.Length > 18 && url.Contains(".tumblr.com") && !url.Contains("www.tumblr.com") && !url.Any(char.IsWhiteSpace) &&
                   (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }

        public static bool IsValidTumblrLikedByUrl(string url)
        {
            return url != null && url.Length > 31 && url.Contains("www.tumblr.com/liked/by/") && !url.Any(char.IsWhiteSpace) &&
                   (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }

        public static bool IsValidTumblrSearchUrl(string url)
        {
            return url != null && url.Length > 29 && url.Contains("www.tumblr.com/search/") && !url.Any(char.IsWhiteSpace) &&
                   (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }
    }
}
