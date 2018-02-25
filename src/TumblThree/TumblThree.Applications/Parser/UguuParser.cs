using System;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
	public class UguuParser : IUguuParser
	{
		public Regex GetUguuUrlRegex()
		{
			return new Regex("(http[A-Za-z0-9_/:.]*a.uguu.se/(.*))");
		}

		public string CreateUguuUrl(string uguuId, string detectedUrl, UguuTypes uguuType)
		{
			string url;
			switch ( uguuType)
			{
				case UguuTypes.Mp4:
					url = @"https://a.uguu.se/" +  uguuId + ".mp4";
					break;
				case UguuTypes.Webm:
					url = @"https://a.uguu.se/" +  uguuId + ".webm";
					break;
				case UguuTypes.Any:
					url = detectedUrl;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			return url;
		}
	}
}
