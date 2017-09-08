using System;
using System.ComponentModel.Composition;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace TumblThree.Applications.Services
{
    [Export(typeof(ISharedCookieService)), Export]
    public class SharedCookieService : ISharedCookieService
    {
        private readonly CookieContainer cookieContainer = new CookieContainer();
        private const int InternetCookieHttponly = 0x2000;

        [DllImport("wininet.dll", SetLastError = true)]
        public static extern bool InternetGetCookieEx(
            string url,
            string cookieName,
            StringBuilder cookieData,
            ref int size,
            int dwFlags,
            IntPtr lpReserved);

        [DllImport("wininet.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool InternetSetCookie(
            string urlName,
            string cookieName,
            string cookieData);

        public void GetUriCookie(CookieContainer request, Uri uri)
        {
            if (cookieContainer.GetCookies(uri).Count == 0)
            {
                foreach (Cookie cookie in GetUriCookieContainer(uri).GetCookies(uri))
                {
                    request.Add(cookie);
                }
            }
        }

        public void SetUriCookie(CookieCollection cookies)
        {
            foreach (Cookie cookie in cookies)
            {
                InternetSetCookie("http://" + cookie.Domain, cookie.Name, cookie.Value);
                cookieContainer.Add(cookie);
            }
        }

        /// <summary>
        ///     Gets the URI cookie container.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        private static CookieContainer GetUriCookieContainer(Uri uri)
        {
            CookieContainer cookies = new CookieContainer();
            var datasize = 0;
            InternetGetCookieEx(uri.ToString(), null, null, ref datasize, InternetCookieHttponly, IntPtr.Zero);
            var cookieData = new StringBuilder(datasize);
            if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, InternetCookieHttponly, IntPtr.Zero))
            {
                if (datasize < 0)
                {
                    return cookies;
                }
                // Allocate stringbuilder large enough to hold the cookie
                cookieData = new StringBuilder(datasize);
                if (!InternetGetCookieEx(
                    uri.ToString(),
                    null, cookieData,
                    ref datasize,
                    InternetCookieHttponly,
                    IntPtr.Zero))
                {
                    return cookies;
                }
            }
            if (cookieData.Length > 0)
            {
                cookies.SetCookies(uri, cookieData.ToString().Replace(';', ','));
            }
            return cookies;
        }

        private static void SetUriCookieContainer(CookieCollection cookies)
        {
            foreach (Cookie cookie in cookies)
            {
                InternetSetCookie("http://" + cookie.Domain, cookie.Name, cookie.Value);
            }
        }
    }
}
