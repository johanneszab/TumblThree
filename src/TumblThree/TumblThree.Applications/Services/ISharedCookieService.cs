using System;
using System.Net;

namespace TumblThree.Applications.Services
{
    public interface ISharedCookieService
    {
        CookieCollection GetUriCookie(Uri uri);

        void SetUriCookie(CookieCollection cookies);
    }
}
