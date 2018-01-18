using System;
using System.Text.RegularExpressions;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
	public class GoogleDriveParser : IGoogleDriveParser
	{
		

		public Regex GetGoogleDriveUrlRegex()
		{
			return new Regex("(http[A-Za-z0-9_/:.]*drive.google.com/(.*))");


		}

		public string CreateGoogleDriveUrl(string id, string fullurl,GoogleDriveTypes type)
		{
			string url;
			switch ( type)
			{
				case GoogleDriveTypes.Any:
					//Init(fullurl);
					url = fullurl;
					break;
				case GoogleDriveTypes.Mp4:
					//not added yet
					url = fullurl;
					break;
				case GoogleDriveTypes.Webm:
					//not added yet
					url = fullurl;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			return url;
		}


	}
}