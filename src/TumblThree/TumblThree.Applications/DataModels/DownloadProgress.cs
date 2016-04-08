using System;

namespace TumblThree.Applications.DataModels
{
    public class DownloadProgress
    {
        public uint ProgressPercentage { get; set; }
        public string Url { get; set; }
        public string Progress { get; set; }
        public uint DownloadedImages { get; set; }
        public uint TotalCount { get; set; }
        public Uri PictureLocation { get; set; }
    }
}
