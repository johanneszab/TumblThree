namespace TumblThree.Domain.Models
{
    public class PostCounter
    {
        public int TotalDownloads;
        public int Photos;
        public int Videos;
        public int Audios;
        public int Texts;
        public int Conversations;
        public int Quotes;
        public int Links;
        public int PhotoMetas;
        public int VideoMetas;
        public int AudioMetas;

        public PostCounter()
        {
        }

        public PostCounter(IBlog blog)
        {
            TotalDownloads = blog.DownloadedImages;
            Photos = blog.DownloadedPhotos;
            Videos = blog.DownloadedVideos;
            Audios = blog.DownloadedAudios;
            Texts = blog.DownloadedTexts;
            Conversations = blog.DownloadedConversations;
            Quotes = blog.DownloadedQuotes;
            Links = blog.DownloadedLinks;
            PhotoMetas = blog.DownloadedPhotoMetas;
            VideoMetas = blog.DownloadedVideoMetas;
            AudioMetas = blog.DownloadedAudioMetas;
        }
    }

}
