using System;
using System.Text.RegularExpressions;
using TumblThree.Domain.Models;
using CG.Web.MegaApiClient;
using System.IO;

namespace TumblThree.Applications.Crawler
{
	public class MegaParser : IMegaParser
	{

		public Regex GetMegaUrlRegex()
		{
	        
			return new Regex("(http[A-Za-z0-9_/:.]*mega.nz/#(.*)([A-Za-z0-9_].*))");
	        
		}





	}
}