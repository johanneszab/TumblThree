using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace TumblThree.Applications.Services
{
    public interface IWebRequestFactory
    {
        HttpWebRequest CreateGetReqeust(string url, string referer = "", Dictionary<string, string> headers = null);

        HttpWebRequest CreateGetXhrReqeust(string url, string referer = "", Dictionary<string, string> headers = null);

        HttpWebRequest CreatePostReqeust(string url, string referer = "", Dictionary<string, string> headers = null);

        HttpWebRequest CreatePostXhrReqeust(string url, string referer = "", Dictionary<string, string> headers = null);

        Task<bool> RemotePageIsValid(string url);

        Task<string> ReadReqestToEnd(HttpWebRequest request);

        Stream GetStreamForApiRequest(Stream stream);

        string UrlEncode(IDictionary<string, string> parameters);
    }
}