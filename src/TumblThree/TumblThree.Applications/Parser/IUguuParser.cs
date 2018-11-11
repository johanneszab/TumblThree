using System.Collections.Generic;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public interface IUguuParser
    {
        Regex GetUguuUrlRegex();

        string GetUguuId(string url);

        string CreateUguuUrl(string uguuId, string detectedUrl, UguuTypes uguuType);

        IEnumerable<string> SearchForUguuUrl(string searchableText, UguuTypes uguuType);
    }
}
