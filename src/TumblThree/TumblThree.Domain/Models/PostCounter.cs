namespace TumblThree.Domain.Models
{
    public class PostCounter
    {
        public int AudioMetas;
        public int Audios;
        public int Conversations;
        public int Links;
        public int PhotoMetas;
        public int Photos;
        public int Quotes;
        public int Texts;
        public int TotalDownloads;
        public int VideoMetas;
        public int Videos;

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
