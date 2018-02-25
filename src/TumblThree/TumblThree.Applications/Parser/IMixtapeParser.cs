using System.Text.RegularExpressions;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
	public interface IMixtapeParser
	{
		Regex GetMixtapeUrlRegex();

		string CreateMixtapeUrl(string mixtapeId, string detectedUrl, MixtapeTypes mixtapeType);
	}
}