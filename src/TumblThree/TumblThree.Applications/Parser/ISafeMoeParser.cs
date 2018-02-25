using System.Text.RegularExpressions;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
	public interface ISafeMoeParser
	{
		Regex GetSafeMoeUrlRegex();

		string CreateSafeMoeUrl(string id, string detectedUrl, SafeMoeTypes type);
	}
}
