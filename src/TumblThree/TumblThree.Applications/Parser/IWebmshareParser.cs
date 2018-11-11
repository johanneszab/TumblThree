using System.Collections.Generic;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public interface IWebmshareParser
    {
        Regex GetWebmshareUrlRegex();

        string GetWebmshareId(string url);

        string CreateWebmshareUrl(string webshareId, string detectedUrl, WebmshareTypes webmshareType);

        IEnumerable<string> SearchForWebmshareUrl(string searchableText, WebmshareTypes webmshareType);
    }
}
