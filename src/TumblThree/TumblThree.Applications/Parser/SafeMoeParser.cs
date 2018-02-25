using System;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
	public class SafeMoeParser : ISafeMoeParser
	{
		public Regex GetSafeMoeUrlRegex()
		{
			return new Regex("(http[A-Za-z0-9_/:.]*a.safe.moe/(.*))");
		}

		public string CreateSafeMoeUrl(string id, string detectedUrl, SafeMoeTypes type)
		{
			string url;
			switch ( type)
			{
				case SafeMoeTypes.Mp4:
					url = @"https://a.safe.moe/" +  id + ".mp4";
					break;
				case SafeMoeTypes.Webm:
					url = @"https://a.safe.moe/" +  id + ".webm";
					break;
				case SafeMoeTypes.Any:
					url = detectedUrl;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			return url;
		}
	}
}
