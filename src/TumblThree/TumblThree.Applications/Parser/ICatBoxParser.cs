using System.Text.RegularExpressions;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
	public interface ICatBoxParser
	{
		Regex GetCatBoxUrlRegex();

		string CreateCatBoxUrl(string id, string detectedUrl, CatBoxTypes type);
	}
}
