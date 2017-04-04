using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TumblThree.Domain.Models
{
    public interface IBlog : INotifyPropertyChanged
    {
        string Name { get; set; }

        string Url { get; set; }

        string Location { get; set; }

        string ChildId { get; set; }

        int Rating { get; set; }

        int Progress { get; set; }

        int Posts { get; set; }

        int Texts { get; set; }

        int Quotes { get; set; }

        int Photos { get; set; }

        int NumberOfLinks { get; set; }

        int Conversations { get; set; }

        int Videos { get; set; }

        int Audios { get; set; }

        int PhotoMetas { get; set; }

        int VideoMetas { get; set; }

        int AudioMetas { get; set; }

        int DownloadedTexts { get; set; }

        int DownloadedQuotes { get; set; }

        int DownloadedPhotos { get; set; }

        int DownloadedLinks { get; set; }

        int DownloadedConversations { get; set; }

        int DownloadedVideos { get; set; }

        int DownloadedAudios { get; set; }

        int DownloadedPhotoMetas { get; set; }

        int DownloadedVideoMetas { get; set; }

        int DownloadedAudioMetas { get; set; }

        int DownloadedImages { get; set; }

        int TotalCount { get; set; }

        string Notes { get; set; }

        bool DownloadUrlList { get; set; }

        string LastDownloadedPhoto { get; set; }

        string LastDownloadedVideo{ get; set; }

        BlogTypes BlogType { get; set; }

        DateTime DateAdded { get; set; }

        DateTime LastCompleteCrawl { get; set; }

        bool Online { get; set; }

        bool CheckDirectoryForFiles { get; set; }

        bool Dirty { get; set; }

        Exception LoadError { get; set; }

        IList<string> Links { get; set; }

        bool Save();

        string DownloadLocation();
    }
}
