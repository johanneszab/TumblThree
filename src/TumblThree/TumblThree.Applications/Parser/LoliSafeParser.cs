using System;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
	public class LoliSafeParser : ILoliSafeParser
	{
		public Regex GetLoliSafeUrlRegex()
		{
			return new Regex("(http[A-Za-z0-9_/:.]*loli.temel.me/(.*))");
		}

		public string CreateLoliSafeUrl(string id, string detectedUrl, LoliSafeTypes type)
		{
			string url;
			switch ( type)
			{
				case LoliSafeTypes.Mp4:
					url = @"https://loli.temel.me/" +  id + ".mp4";
					break;
				case LoliSafeTypes.Webm:
					url = @"https://loli.temel.me/" +  id + ".webm";
					break;
				case LoliSafeTypes.Any:
					url = detectedUrl;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			return url;
		}
	}
}
