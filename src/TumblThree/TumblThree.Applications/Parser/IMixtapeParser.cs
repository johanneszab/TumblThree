using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public interface IMixtapeParser
    {
        Regex GetMixtapeUrlRegex();

        string CreateMixtapeUrl(string mixtapeId, string detectedUrl, MixtapeTypes mixtapeType);
    }
}
