using System;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
	public class CatBoxParser : ICatBoxParser
	{
		public Regex GetCatBoxUrlRegex()
		{
			return new Regex("(http[A-Za-z0-9_/:.]*files.catbox.moe/(.*))");
		}

		public string CreateCatBoxUrl(string id, string detectedUrl, CatBoxTypes type)
		{
			string url;
			switch ( type)
			{
				case CatBoxTypes.Mp4:
					url = @"https://files.catbox.moe/" +  id + ".mp4";
					break;
				case CatBoxTypes.Webm:
					url = @"https://files.catbox.moe/" +  id + ".webm";
					break;
				case CatBoxTypes.Any:
					url = detectedUrl;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			return url;
		}
	}
}
