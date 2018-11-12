using System;
using System.Globalization;

namespace TumblThree.Applications.Services
{
    public static class ShellServiceExtensions
    {
        public static void ShowError(this IShellService shellService, Exception exception, string format, params object[] args) =>
            shellService.ShowError(exception, string.Format(CultureInfo.CurrentCulture, format, args));
    }
}
