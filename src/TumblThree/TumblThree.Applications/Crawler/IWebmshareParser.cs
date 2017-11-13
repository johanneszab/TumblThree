using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
    public interface IWebmshareParser
    {
        string CreateWebmshareUrl(string webshareId, WebmshareTypes webmshareType);
    }
}