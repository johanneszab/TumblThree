using System;
using System.Net;

namespace TumblThree.Applications.Services
{
    public interface ISharedCookieService
    {
        void GetUriCookie(CookieContainer request, Uri uri);

        void GetTumblrToSCookie(CookieContainer request, Uri uri);

        void SetUriCookie(CookieCollection cookies);
    }
}
