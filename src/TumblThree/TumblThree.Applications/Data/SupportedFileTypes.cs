using System.Collections.Generic;

namespace TumblThree.Applications.Data
{
    internal static class SupportedFileTypes
    {
        private static readonly string[] blogFileExtensions = new string[] { ".tumblr", ".insta", ".twitter" };
        private static readonly string[] queueFileExtensions = new string[] { ".que" };

        public static IReadOnlyList<string> BlogFileExtensions { get { return blogFileExtensions; } }

        public static IReadOnlyList<string> QueueFileExtensions { get { return queueFileExtensions; } }
    }
}
