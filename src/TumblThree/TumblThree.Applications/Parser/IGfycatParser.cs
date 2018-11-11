using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public interface IGfycatParser
    {
        Regex GetGfycatUrlRegex();

        string GetGfycatId(string url);

        Task<string> RequestGfycatCajax(string gfyId);

        string ParseGfycatCajaxResponse(string result, GfycatTypes gfycatType);

        Task<IEnumerable<string>> SearchForGfycatUrlAsync(string searchableText, GfycatTypes gfycatType);
    }
}
