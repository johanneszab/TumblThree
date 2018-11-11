using System.Collections.Generic;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public interface IMixtapeParser
    {
        Regex GetMixtapeUrlRegex();

        string GetMixtapeId(string url);

        string CreateMixtapeUrl(string mixtapeId, string detectedUrl, MixtapeTypes mixtapeType);

        IEnumerable<string> SearchForMixtapeUrl(string searchableText, MixtapeTypes mixtapeType);
    }
}
