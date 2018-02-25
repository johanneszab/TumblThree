using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TumblThree.Applications.Parser
{
	public interface IMegaParser
	{
        Task Login();

        Task Logout();

        Regex GetMegaUrlRegex();

        Task<IEnumerable<string>> GetUrls(string url);
    }
}