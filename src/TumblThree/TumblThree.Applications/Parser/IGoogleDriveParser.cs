using System.Text.RegularExpressions;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
	public interface IGoogleDriveParser
	{
		Regex GetGoogleDriveUrlRegex();

		string CreateGoogleDriveUrl(string id, string fullurl, GoogleDriveTypes type);
	}
}