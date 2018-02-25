using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TumblThree.Applications.Parser;
using CG.Web.MegaApiClient;
using System.Collections.Generic;
using System.Linq;

namespace TumblThree.Applications.Crawler
{
	public class MegaParser : IMegaParser
	{
        private MegaApiClient client;

        public MegaParser(MegaApiClient client)
        {
            this.client = client;
        }

        public async Task Login()
        {
            await client.LoginAnonymousAsync();
        }

        public async Task Logout()
        {
            await client.LogoutAsync();
        }

        public Regex GetMegaUrlRegex()
		{
			return new Regex("(http[A-Za-z0-9_/:.]*mega.nz/#(.*)([A-Za-z0-9_].*))");        
		}

        public async Task<IEnumerable<string>> GetUrls (string url)
        {
            await Login();

            List<string> urls = new List<string>();
            List<string> names = new List<string>();

            IEnumerable<INode> nodes = await client.GetNodesFromLinkAsync(new Uri(url));

            List<INode> fileNodes = nodes.Where(n => n.Type == NodeType.File).ToList();
            foreach (var node in fileNodes)
            {
                urls.Add((await client.GetDownloadLinkAsync(node)).ToString());
                names.Add(node.Name);
            }

            await Logout();

            return urls;
        }
    }
}