using System.ComponentModel.Composition;
using System.Linq;

namespace TumblThree.Domain.Models
{
    [Export(typeof(IUrlValidator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class UrlValidator : IUrlValidator
    {
        public bool IsValidTumblrUrl(string url)
        {
            return url != null && url.Length > 18 && url.Contains(".tumblr.com") && !url.Contains("//www.tumblr.com") && !url.Any(char.IsWhiteSpace) &&
                   (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }

        public bool IsValidTumblrHiddenUrl(string url)
        {
            return url != null && url.Length > 38 && url.Contains("www.tumblr.com/dashboard/blog/") && !url.Any(char.IsWhiteSpace) &&
                   (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }

        public bool IsValidTumblrLikedByUrl(string url)
        {
            return url != null && url.Length > 31 && url.Contains("www.tumblr.com/liked/by/") && !url.Any(char.IsWhiteSpace) &&
                   (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }

        public bool IsValidTumblrSearchUrl(string url)
        {
            return url != null && url.Length > 29 && url.Contains("www.tumblr.com/search/") && !url.Any(char.IsWhiteSpace) &&
                   (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }

        public bool IsValidTumblrTagSearchUrl(string url)
        {
            return url != null && url.Length > 29 && url.Contains("www.tumblr.com/tagged/") && !url.Any(char.IsWhiteSpace) &&
                   (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }

        public string AddHttpsProtocol(string url)
        {
            if (url == null)
                return string.Empty;
            if (!url.Contains("http"))
                return "https://" + url;
            return url;
        }
    }
}
