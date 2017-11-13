using System.Threading.Tasks;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
    public interface IGfycatParser
    {
        Task<string> RequestGfycatCajax(string gfyId);

        string ParseGfycatCajaxResponse(string result, GfycatTypes gfycatType);
    }
}