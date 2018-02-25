using System.ComponentModel.Composition;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Services
{
    /// <summary>
    /// </summary>
    [Export(typeof(ISettingsService))]
    public class SettingsService : ISettingsService
    {
        private readonly IShellService shellService;

        [ImportingConstructor]
        public SettingsService(IShellService shellService)
        {
            this.shellService = shellService;
        }

        public IBlog TransferGlobalSettingsToBlog(IBlog blog)
        {
            blog.DownloadAudio = shellService.Settings.DownloadAudios;
            blog.DownloadPhoto = shellService.Settings.DownloadImages;
            blog.DownloadVideo = shellService.Settings.DownloadVideos;
            blog.DownloadText = shellService.Settings.DownloadTexts;
            blog.DownloadAnswer = shellService.Settings.DownloadAnswers;
            blog.DownloadQuote = shellService.Settings.DownloadQuotes;
            blog.DownloadConversation = shellService.Settings.DownloadConversations;
            blog.DownloadLink = shellService.Settings.DownloadLinks;
            blog.CreatePhotoMeta = shellService.Settings.CreateImageMeta;
            blog.CreateVideoMeta = shellService.Settings.CreateVideoMeta;
            blog.CreateAudioMeta = shellService.Settings.CreateAudioMeta;
            blog.MetadataFormat = shellService.Settings.MetadataFormat;
            blog.SkipGif = shellService.Settings.SkipGif;
            blog.DownloadRebloggedPosts = shellService.Settings.DownloadRebloggedPosts;
            blog.ForceSize = shellService.Settings.ForceSize;
            blog.CheckDirectoryForFiles = shellService.Settings.CheckDirectoryForFiles;
            blog.DownloadUrlList = shellService.Settings.DownloadUrlList;
            blog.DownloadPages = shellService.Settings.DownloadPages;
            blog.PageSize = shellService.Settings.PageSize;
            blog.DownloadFrom = shellService.Settings.DownloadFrom;
            blog.DownloadTo = shellService.Settings.DownloadTo;
            blog.Tags = shellService.Settings.Tags;
            blog.DownloadImgur = shellService.Settings.DownloadImgur;
            blog.DownloadGfycat = shellService.Settings.DownloadGfycat;
            blog.DownloadWebmshare = shellService.Settings.DownloadWebmshare;
            blog.DownloadMixtape = shellService.Settings.DownloadMixtape;
            blog.DownloadUguu = shellService.Settings.DownloadUguu;
            blog.DownloadSafeMoe = shellService.Settings.DownloadSafeMoe;
            blog.DownloadLoliSafe = shellService.Settings.DownloadLoliSafe;
            blog.DownloadCatBox = shellService.Settings.DownloadCatBox;
            blog.GfycatType = shellService.Settings.GfycatType;
            blog.WebmshareType = shellService.Settings.WebmshareType;
            blog.MixtapeType = shellService.Settings.MixtapeType;
            blog.UguuType = shellService.Settings.UguuType;
            blog.SafeMoeType = shellService.Settings.SafeMoeType;
            blog.LoliSafeType = shellService.Settings.LoliSafeType;
            blog.CatBoxType = shellService.Settings.CatBoxType;
            blog.DumpCrawlerData = shellService.Settings.DumpCrawlerData;
            return blog;
        }
    }
}
