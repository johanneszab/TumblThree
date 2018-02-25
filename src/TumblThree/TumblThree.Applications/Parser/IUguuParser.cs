using System.Text.RegularExpressions;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
	public interface IUguuParser
	{
		Regex GetUguuUrlRegex();

		string CreateUguuUrl(string uguuId, string detectedUrl, UguuTypes uguuType);
	}
}
