using System.Collections.Generic;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public interface ICatBoxParser
    {
        Regex GetCatBoxUrlRegex();

        string GetCatBoxId(string url);

        string CreateCatBoxUrl(string id, string detectedUrl, CatBoxTypes type);

        IEnumerable<string> SearchForCatBoxUrl(string searchableText, CatBoxTypes catBoxType);
    }
}
