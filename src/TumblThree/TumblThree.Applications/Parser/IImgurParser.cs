using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TumblThree.Applications.Parser
{
    public interface IImgurParser
    {
        string GetImgurId(string url);

        Regex GetImgurImageRegex();

        Regex GetImgurAlbumRegex();

        Regex GetImgurAlbumHashRegex();

        Regex GetImgurAlbumExtRegex();

        Task<string> RequestImgurAlbumSite(string gfyId);

        IEnumerable<string> SearchForImgurUrl(string searchableText);

        Task<IEnumerable<string>> SearchForImgurUrlFromAlbumAsync(string searchableText);
    }
}
