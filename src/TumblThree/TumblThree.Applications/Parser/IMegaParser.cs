using System.Text.RegularExpressions;
using CG.Web.MegaApiClient;
using System.IO;
using TumblThree.Domain.Models;
using System;

namespace TumblThree.Applications.Crawler
{
	
	public enum MegaLinkType
	{
		Single,
		Folder
	}

	public interface IMegaParser
	{

		Regex GetMegaUrlRegex();


		string CreateMegaUrl(string id, string fullurl,MegaTypes type);
		
	}
}