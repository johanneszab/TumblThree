using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public interface IWebmshareParser
    {
        Regex GetWebmshareUrlRegex();

        string CreateWebmshareUrl(string webshareId, string detectedUrl, WebmshareTypes webmshareType);
    }
}
