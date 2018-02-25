using System.Text.RegularExpressions;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
	public interface ILoliSafeParser
	{
		Regex GetLoliSafeUrlRegex();

		string CreateLoliSafeUrl(string id, string detectedUrl, LoliSafeTypes type);
	}
}
