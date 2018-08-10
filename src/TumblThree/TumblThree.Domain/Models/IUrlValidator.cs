namespace TumblThree.Domain.Models
{
    public interface IUrlValidator
    {
        string AddHttpsProtocol(string url);
        bool IsValidTumblrHiddenUrl(string url);
        bool IsValidTumblrLikedByUrl(string url);
        bool IsValidTumblrSearchUrl(string url);
        bool IsValidTumblrTagSearchUrl(string url);
        bool IsValidTumblrUrl(string url);
    }
}