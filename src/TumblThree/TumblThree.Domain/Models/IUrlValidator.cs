namespace TumblThree.Domain.Models
{
    public interface IUrlValidator
    {
        bool IsValidTumblrHiddenUrl(string url);

        bool IsValidTumblrLikedByUrl(string url);

        bool IsValidTumblrSearchUrl(string url);

        bool IsValidTumblrTagSearchUrl(string url);

        bool IsValidTumblrUrl(string url);

        string AddHttpsProtocol(string url);

        bool IsTumbexUrl(string url);
    }
}
