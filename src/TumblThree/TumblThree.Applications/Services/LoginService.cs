using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TumblThree.Applications.Crawler;
using TumblThree.Applications.Extensions;

namespace TumblThree.Applications.Services
{
    [Export, Export(typeof(ILoginService))]
    internal class LoginService : ILoginService
    {
        private readonly IShellService shellService;
        private readonly ISharedCookieService cookieService;
        private readonly IWebRequestFactory webRequestFactory;
        private string tumblrKey = string.Empty;

        [ImportingConstructor]
        public LoginService(IShellService shellService, IWebRequestFactory webRequestFactory, ISharedCookieService cookieService)
        {
            this.shellService = shellService;
            this.webRequestFactory = webRequestFactory;
            this.cookieService = cookieService;
        }

        public async Task PerformTumblrLogin(string login, string password)
        {
            string document = await RequestTumblrKey();
            tumblrKey = ExtractTumblrKey(document);
            await Register(login, password);
            await Authenticate(login, password);
        }

        private static string ExtractTumblrKey(string document)
        {
            return Regex.Match(document, "id=\"tumblr_form_key\" content=\"([\\S]*)\">").Groups[1].Value;
        }

        private async Task<string> RequestTumblrKey()
        {
            string url = "https://www.tumblr.com/login";
            HttpWebRequest request = webRequestFactory.CreateGetReqeust(url);
            cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
            using (var response = await request.GetResponseAsync() as HttpWebResponse)
            {
                cookieService.SetUriCookie(response.Cookies);
                using (var stream = webRequestFactory.GetStreamForApiRequest(response.GetResponseStream()))
                {
                    using (var buffer = new BufferedStream(stream))
                    {
                        using (var reader = new StreamReader(buffer))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }

        protected async Task Register(string login, string password)
        {
            string url = "https://www.tumblr.com/svc/account/register";
            string referer = "https://www.tumblr.com/login";
            var headers = new Dictionary<string, string>();
            HttpWebRequest request = webRequestFactory.CreatePostXhrReqeust(url, referer, headers);
            request.AllowAutoRedirect = true;
            cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
            var parameters = new Dictionary<string, string>
            {
                { "determine_email", login },
                { "user[email]", "" },
                { "user[password]", "" },
                { "tumblelog[name]", "" },
                { "user[age]", "" },
                { "context", "no_referer" },
                { "version", "STANDARD" },
                { "follow", "" },
                { "form_key", tumblrKey },
                { "seen_suggestion", "0" },
                { "used_suggestion", "0" },
                { "used_auto_suggestion", "0" },
                { "about_tumblr_slide", "" },
                { "tracking_url", "/login" },
                { "tracking_version", "modal" },
                { "random_username_suggestions", "[\"KawaiiBouquetStranger\",\"KeenTravelerFury\",\"RainyMakerTastemaker\",\"SuperbEnthusiastCollective\",\"TeenageYouthFestival\"]" },
                { "action", "signup_determine" },
            };
            string requestBody = webRequestFactory.UrlEncode(parameters);
            using (Stream postStream = await request.GetRequestStreamAsync())
            {
                byte[] postBytes = Encoding.ASCII.GetBytes(requestBody);
                await postStream.WriteAsync(postBytes, 0, postBytes.Length);
                await postStream.FlushAsync();
            }
            using (var response = await request.GetResponseAsync() as HttpWebResponse)
            {
                cookieService.SetUriCookie(response.Cookies);
            }
        }

        protected async Task Authenticate(string login, string password)
        {
            string url = "https://www.tumblr.com/login";
            string referer = "https://www.tumblr.com/login";
            var headers = new Dictionary<string, string>();
            HttpWebRequest request = webRequestFactory.CreatePostReqeust(url, referer, headers);
            request.AllowAutoRedirect = true;
            cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
            var parameters = new Dictionary<string, string>
            {
                { "determine_email", login },
                { "user[email]", login },
                { "user[password]", password },
                { "tumblelog[name]", "" },
                { "user[age]", "" },
                { "context", "no_referer" },
                { "version", "STANDARD" },
                { "follow", "" },
                { "form_key", tumblrKey },
                { "seen_suggestion", "0" },
                { "used_suggestion", "0" },
                { "used_auto_suggestion", "0" },
                { "about_tumblr_slide", "" },
                { "random_username_suggestions", "[\"KawaiiBouquetStranger\",\"KeenTravelerFury\",\"RainyMakerTastemaker\",\"SuperbEnthusiastCollective\",\"TeenageYouthFestival\"]" },
                { "action", "signup_determine" }
            };
            string requestBody = webRequestFactory.UrlEncode(parameters);
            using (Stream postStream = await request.GetRequestStreamAsync())
            {
                byte[] postBytes = Encoding.ASCII.GetBytes(requestBody);
                await postStream.WriteAsync(postBytes, 0, postBytes.Length);
                await postStream.FlushAsync();
            }
            using (var response = await request.GetResponseAsync() as HttpWebResponse)
            {
                cookieService.SetUriCookie(request.CookieContainer.GetCookies(new Uri("https://www.tumblr.com/")));
                //cookieService.SetUriCookie(response.Cookies);
            }
        }
    }
}
