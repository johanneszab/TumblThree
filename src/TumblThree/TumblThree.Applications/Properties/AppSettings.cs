using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace TumblThree.Applications.Properties
{
    public sealed class AppSettings : IExtensibleDataObject
    {
        public AppSettings()
        {
            Initialize();
        }

        public static string[] imageSizes =
            new string[] {
                "1280", "500", "400", "250", "100", "75"
        };

        [DataMember]
        public double Left { get; set; }

        [DataMember]
        public double Top { get; set; }

        [DataMember]
        public double Height { get; set; }

        [DataMember]
        public double Width { get; set; }

        [DataMember]
        public bool IsMaximized { get; set; }

        [DataMember]
        public string DownloadLocation { get; set; }

        [DataMember]
        public int ParallelImages { get; set; }

        [DataMember]
        public int ParallelBlogs { get; set; }

        [DataMember]
        public int ImageSize { get; set; }

        [DataMember]
        public bool CheckClipboard { get; set; }

        [DataMember]
        public bool ShowPicturePreview { get; set; }

        [DataMember]
        public bool DeleteOnlyIndex { get; set; }

        [DataMember]
        public bool CheckOnlineStatusAtStartup { get; set; }

        [DataMember]
        public bool SkipGif { get; set; }

        [DataMember]
        public bool RemoveIndexAfterCrawl { get; set; }

        ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }

        public ObservableCollection<string> ImageSizes
        {
            get
            {
                return new ObservableCollection<string>(imageSizes);
            }
        }

        private void Initialize()
        {
            Left = 50;
            Top = 50;
            Height = 800;
            Width = 1200;
            IsMaximized = false;
            DownloadLocation = @".\Blogs\";
            ParallelImages = 25;
            ParallelBlogs = 2;
            ImageSize = 1280;
            CheckClipboard = true;
            ShowPicturePreview = true;
            DeleteOnlyIndex = true;
            CheckOnlineStatusAtStartup = true;
            SkipGif = false;
            RemoveIndexAfterCrawl = false;
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
        }
    }
}
