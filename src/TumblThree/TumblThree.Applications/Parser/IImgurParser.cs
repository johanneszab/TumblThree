using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TumblThree.Applications.Parser
{
    public interface IImgurParser
    {
        Regex GetImgurImageRegex();

        Regex GetImgurAlbumRegex();

        Regex GetImgurAlbumHashRegex();

        Regex GetImgurAlbumExtRegex();

        Task<string> RequestImgurAlbumSite(string gfyId);
    }
}
