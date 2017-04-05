using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace TumblThree.Domain
{
    public static class Logger
    {
        [Conditional("DEBUG")]
        public static void Verbose(string format, params object[] arguments)
        {
            Debug.WriteLine("> {0:HH:mm:ss.fff} > {1}", DateTime.Now,
                string.Format(CultureInfo.InvariantCulture, format, arguments));
        }

        public static void Information(string format, params object[] arguments)
        {
            Trace.TraceInformation(format, arguments);
        }

        public static void Warning(string format, params object[] arguments)
        {
            Trace.TraceWarning(format, arguments);
        }

        public static void Error(string format, params object[] arguments)
        {
            Trace.TraceError(format, arguments);
        }

        public static string GetMemberName([CallerMemberName] string memberName = null)
        {
            return memberName;
        }
    }
}
