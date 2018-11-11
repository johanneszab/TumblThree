using System.Collections.Generic;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public interface ISafeMoeParser
    {
        Regex GetSafeMoeUrlRegex();

        string GetSafeMoeId(string url);

        string CreateSafeMoeUrl(string id, string detectedUrl, SafeMoeTypes type);

        IEnumerable<string> SearchForSafeMoeUrl(string searchableText, SafeMoeTypes safeMoeType);
    }
}
