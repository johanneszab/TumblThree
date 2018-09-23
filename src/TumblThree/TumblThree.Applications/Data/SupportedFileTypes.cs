using System.Collections.Generic;

namespace TumblThree.Applications.Data
{
    internal static class SupportedFileTypes
    {
        private static readonly string[] blogFileExtensions = new string[] { ".tumblr", ".insta", ".twitter" };
        private static readonly string[] queueFileExtensions = new string[] { ".que" };
        private static readonly string[] bloglistExportFileType = new string[] { ".txt" };

        public static IReadOnlyList<string> BlogFileExtensions => blogFileExtensions;

        public static IReadOnlyList<string> QueueFileExtensions => queueFileExtensions;

        public static IReadOnlyList<string> BloglistExportFileType => bloglistExportFileType;

    }
}
