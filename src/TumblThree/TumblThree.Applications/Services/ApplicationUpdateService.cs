using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Xml;
using System.Xml.Linq;

using TumblThree.Domain;

namespace TumblThree.Applications.Services
{
    /// <summary>
    /// </summary>
    [Export(typeof(IApplicationUpdateService))]
    public class ApplicationUpdateService : IApplicationUpdateService
    {
        private readonly IShellService shellService;
        private string downloadLink;
        private string version;

        [ImportingConstructor]
        public ApplicationUpdateService(IShellService shellService)
        {
            this.shellService = shellService;
        }

        private HttpWebRequest CreateWebReqeust(string url)
        {
            var request =
                WebRequest.Create(url)
                    as HttpWebRequest;
            request.Method = "GET";
            request.ProtocolVersion = HttpVersion.Version11;
            request.ContentType = "application/json";
            request.ServicePoint.Expect100Continue = false;
            request.UnsafeAuthenticatedConnectionSharing = true;
            request.UserAgent = shellService.Settings.UserAgent;
            //request.KeepAlive = true;
            //request.Pipelined = true;
            if (!string.IsNullOrEmpty(shellService.Settings.ProxyHost) && !string.IsNullOrEmpty(shellService.Settings.ProxyPort))
            {
                request.Proxy = new WebProxy(shellService.Settings.ProxyHost, int.Parse(shellService.Settings.ProxyPort));
            }
            if (!string.IsNullOrEmpty(shellService.Settings.ProxyUsername) && !string.IsNullOrEmpty(shellService.Settings.ProxyPassword))
            {
                request.Proxy.Credentials = new NetworkCredential(shellService.Settings.ProxyUsername, shellService.Settings.ProxyPassword);
            }
            return request;
        }

        public async Task<string> GetLatestReleaseFromServer()
        {
            version = null;
            downloadLink = null;
            try
            {
                var request = CreateWebReqeust(@"https://api.github.com/repos/johanneszab/tumblthree/releases/latest");
                string result;
                using (var resp = await request.GetResponseAsync() as HttpWebResponse)
                {
                    var reader =
                        new StreamReader(resp.GetResponseStream());
                    result = reader.ReadToEnd();
                }
                XmlDictionaryReader jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(result),
                    new System.Xml.XmlDictionaryReaderQuotas());
                XElement root = XElement.Load(jsonReader);
                version = root.Element("tag_name").Value;
                downloadLink = root.Element("assets").Element("item").Element("browser_download_url").Value;
            }
            catch (Exception exception)
            {
                Logger.Error(exception.ToString());
                return exception.Message;
            }
            return null;
        }

        public bool IsNewVersionAvailable()
        {
            try
            {
                var newVersion = new Version(version.Substring(1));
                if (newVersion > new Version(ApplicationInfo.Version))
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception.ToString());
            }
            return false;
        }

        public string GetNewAvailableVersion()
        {
            return version;
        }

        public Uri GetDownloadUri()
        {
            if (downloadLink == null)
            {
                return null;
            }
            return new Uri(downloadLink);
        }
    }
}
