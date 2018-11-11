using System.Collections.Generic;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public interface ILoliSafeParser
    {
        Regex GetLoliSafeUrlRegex();

        string GetLoliSafeId(string url);

        string CreateLoliSafeUrl(string id, string detectedUrl, LoliSafeTypes type);

        IEnumerable<string> SearchForLoliSafeUrl(string searchableText, LoliSafeTypes loliSafeType);
    }
}
