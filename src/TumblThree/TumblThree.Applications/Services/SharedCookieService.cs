using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace TumblThree.Applications.Services
{
    public class SharedCookieService
    {
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

        /// <summary>
        ///     Gets the URI cookie container.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public static CookieContainer GetUriCookieContainer(Uri uri)
        {
            CookieContainer cookies = null;
            var datasize = 0;
            InternetGetCookieEx(uri.ToString(), null, null, ref datasize, InternetCookieHttponly, IntPtr.Zero);
            var cookieData = new StringBuilder(datasize);
            if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, InternetCookieHttponly, IntPtr.Zero))
            {
                if (datasize < 0)
                {
                    return null;
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
                    return null;
                }
            }
            if (cookieData.Length > 0)
            {
                cookies = new CookieContainer();
                cookies.SetCookies(uri, cookieData.ToString().Replace(';', ','));
            }
            return cookies;
        }

        public static void SetUriCookieContainer(CookieCollection cookies)
        {
            foreach (Cookie cookie in cookies)
            {
                InternetSetCookie("http://" + cookie.Domain, cookie.Name, cookie.Value);
            }
        }
    }
}
