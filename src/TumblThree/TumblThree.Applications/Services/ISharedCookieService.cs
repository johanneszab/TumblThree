using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace TumblThree.Applications.Services
{
    public interface ISharedCookieService
    {
        IEnumerable<Cookie> GetAllCookies();

        void GetUriCookie(CookieContainer request, Uri uri);

        void SetUriCookie(IEnumerable cookies);

        void RemoveUriCookie(Uri uri);
    }
}
