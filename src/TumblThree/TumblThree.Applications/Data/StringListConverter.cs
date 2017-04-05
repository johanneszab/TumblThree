using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TumblThree.Applications.Data
{
    public static class StringListConverter
    {
        public static string ToString(IEnumerable<string> list, string separator = null)
        {
            return string.Join(GetSeparator(separator), list ?? new string[0]);
        }

        public static IEnumerable<string> FromString(string text, string separator = null)
        {
            return (text ?? "").Split(new[] { GetSeparator(separator).Trim(' ') }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(x => x.Trim(' ')).Where(x => !string.IsNullOrEmpty(x)).ToArray();
        }

        private static string GetSeparator(string separator)
        {
            return !string.IsNullOrEmpty(separator) ? separator : CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ";
        }
    }
}
