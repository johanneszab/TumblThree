using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Downloader
{
    [Export(typeof(IDownloader))]
    [ExportMetadata("BlogType", BlogTypes.tlb)]
    public class TumblrLikedByDownloader : Downloader, IDownloader
    {
        private readonly IBlog blog;
        private readonly ICrawlerService crawlerService;
        private readonly IShellService shellService;
        private int numberOfPagesCrawled = 0;

        public TumblrLikedByDownloader(IShellService shellService, ICrawlerService crawlerService, IBlog blog)
            : base(shellService, crawlerService, blog)
        {
            this.shellService = shellService;
            this.crawlerService = crawlerService;
            this.blog = blog;
        }

        public async Task Crawl(IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            Logger.Verbose("TumblrLikedByDownloader.Crawl:Start");

            Task grabber = GetUrlsAsync(progress, ct, pt);
            Task<bool> downloader = DownloadBlogAsync(progress, ct, pt);

            await grabber;

            UpdateProgressQueueInformation(progress, Resources.ProgressUniqueDownloads);
            blog.DuplicatePhotos = DetermineDuplicates(PostTypes.Photo);
            blog.DuplicateVideos = DetermineDuplicates(PostTypes.Video);
            blog.DuplicateAudios = DetermineDuplicates(PostTypes.Audio);
            blog.TotalCount = (blog.TotalCount - blog.DuplicatePhotos - blog.DuplicateAudios - blog.DuplicateVideos);

            CleanCollectedBlogStatistics();

            await downloader;

            if (!ct.IsCancellationRequested)
            {
                blog.LastCompleteCrawl = DateTime.Now;
            }

            blog.Save();

            UpdateProgressQueueInformation(progress, "");
        }

        private string ResizeTumblrImageUrl(string imageUrl)
        {
            var sb = new StringBuilder(imageUrl);
            return sb
                .Replace("_1280", "_" + shellService.Settings.ImageSize)
                .Replace("_1280", "_" + shellService.Settings.ImageSize)
                .Replace("_540", "_" + shellService.Settings.ImageSize)
                .Replace("_500", "_" + shellService.Settings.ImageSize)
                .Replace("_400", "_" + shellService.Settings.ImageSize)
                .Replace("_250", "_" + shellService.Settings.ImageSize)
                .Replace("_100", "_" + shellService.Settings.ImageSize)
                .Replace("_75sq", "_" + shellService.Settings.ImageSize)
                .ToString();
        }

        protected override bool CheckIfFileExistsInDirectory(string url)
        {
            string fileName = url.Split('/').Last();
            Monitor.Enter(lockObjectDirectory);
            string blogPath = blog.DownloadLocation();
            if (Directory.EnumerateFiles(blogPath).Any(file => file.Contains(fileName)))
            {
                Monitor.Exit(lockObjectDirectory);
                return true;
            }
            Monitor.Exit(lockObjectDirectory);
            return false;
        }

        private int DetermineDuplicates(PostTypes type)
        {
            return statisticsBag.Where(url => url.PostType.Equals(type))
                                .GroupBy(url => url.Url)
                                .Where(g => g.Count() > 1)
                                .Sum(g => g.Count() - 1);
        }

        protected override bool CheckIfFileExistsInDB(string url)
        {
            string fileName = url.Split('/').Last();
            Monitor.Enter(lockObjectDb);
            if (files.Links.Contains(fileName))
            {
                Monitor.Exit(lockObjectDb);
                return true;
            }
            Monitor.Exit(lockObjectDb);
            return false;
        }

        private async Task GetUrlsAsync(IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            var semaphoreSlim = new SemaphoreSlim(shellService.Settings.ParallelScans);
            var trackedTasks = new List<Task>();

            foreach (int crawlerNumber in Enumerable.Range(0, shellService.Settings.ParallelScans))
            {
                await semaphoreSlim.WaitAsync();

                trackedTasks.Add(new Func<Task>(async () =>
                {
                    try
                    {
                        string document = await RequestDataAsync(blog.Url + "/page/" + crawlerNumber);
                        if (!CheckIfLoggedIn(document))
                        {
                            Logger.Error("TumblrLikedByDownloader:GetUrlsAsync: {0}", "User not logged in");
                            shellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, blog.Name);
                            return;
                        }

                        await AddUrlsToDownloadList(document, progress, crawlerNumber, ct, pt);
                    }
                    catch (WebException)
                    {
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }
                })());
            }
            await Task.WhenAll(trackedTasks);

            producerConsumerCollection.CompleteAdding();

            if (!ct.IsCancellationRequested)
            {
                UpdateBlogStats();
            }
        }

        private bool CheckIfLoggedIn(string document)
        {
            return !document.Contains("<div class=\"signup_view account login\"");
        }

        private async Task AddUrlsToDownloadList(string document, IProgress<DownloadProgress> progress, int crawlerNumber, CancellationToken ct, PauseToken pt)
        {
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }
                if (pt.IsPaused)
                {
                    pt.WaitWhilePausedWithResponseAsyc().Wait();
                }

                var tags = new List<string>();
                if (!string.IsNullOrWhiteSpace(blog.Tags))
                {
                    tags = blog.Tags.Split(',').Select(x => x.Trim()).ToList();
                }

                AddPhotoUrlToDownloadList(document, tags);
                AddVideoUrlToDownloadList(document, tags);

                Interlocked.Increment(ref numberOfPagesCrawled);
                UpdateProgressQueueInformation(progress, Resources.ProgressGetUrlShort, numberOfPagesCrawled);
                crawlerNumber += shellService.Settings.ParallelScans;
                document = await RequestDataAsync(blog.Url + "/page/" + crawlerNumber);
                if (document.Contains("<div class=\"no_posts_found\">"))
                {
                    return;
                }
            }
        }

        protected override async Task DownloadPhotoAsync(IProgress<DataModels.DownloadProgress> progress, TumblrPost downloadItem, CancellationToken ct)
        {
            string url = Url(downloadItem);
            url = TestImageRawUrl(url, shellService.Settings);
            if (!(CheckIfFileExistsInDB(url) || CheckIfBlogShouldCheckDirectory(GetCoreImageUrl(url))))
            {
                string blogDownloadLocation = blog.DownloadLocation();
                string fileName = url.Split('/').Last();
                string fileLocation = FileLocation(blogDownloadLocation, fileName);
                string fileLocationUrlList = FileLocationLocalized(blogDownloadLocation, Resources.FileNamePhotos);
                DateTime postDate = PostDate(downloadItem);
                UpdateProgressQueueInformation(progress, Resources.ProgressDownloadImage, fileName);
                if (await DownloadBinaryFile(fileLocation, fileLocationUrlList, url, ct))
                {
                    SetFileDate(fileLocation, postDate);
                    UpdateBlogPostCount(ref counter.Photos, value => blog.DownloadedPhotos = value);
                    UpdateBlogProgress(ref counter.TotalDownloads);
                    UpdateBlogDB(fileName);
                    if (shellService.Settings.EnablePreview)
                    {
                        if (!fileName.EndsWith(".gif"))
                        {
                            blog.LastDownloadedPhoto = Path.GetFullPath(fileLocation);
                        }
                        else
                        {
                            blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);
                        }
                    }
                }
                else
                {
                    //TODO: Refactoring code! Same as above, just with different url. Note: What Url to store in the files DB?
                    url = TestImageRawUrl(url, shellService.Settings);
                    fileName = url.Split('/').Last();
                    fileLocation = FileLocation(blogDownloadLocation, fileName);
                    UpdateProgressQueueInformation(progress, Resources.ProgressDownloadImage, fileName);
                    if (await DownloadBinaryFile(fileLocation, fileLocationUrlList, url, ct))
                    {
                        SetFileDate(fileLocation, postDate);
                        UpdateBlogPostCount(ref counter.Photos, value => blog.DownloadedPhotos = value);
                        UpdateBlogProgress(ref counter.TotalDownloads);
                        UpdateBlogDB(fileName);
                        if (shellService.Settings.EnablePreview)
                        {
                            if (!fileName.EndsWith(".gif"))
                            {
                                blog.LastDownloadedPhoto = Path.GetFullPath(fileLocation);
                            }
                            else
                            {
                                blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);
                            }
                        }
                    }
                }
            }
        }

        public string TestImageRawUrl(string url, AppSettings settings)
        {
            if (settings.ImageSize == "raw")
            {
                return TestTumblrRawUrl(url, settings);
            }
            return url;
        }

        /// <summary>
        /// Instead of testing for valid urls, we just assume they are valid (skips valuable request since the cause high latency).
        /// The https://media.tumblr.com/ is the most likely host to deliver _raw images followed by the 
        /// https://68.media.tumblr.com/ host. We only test the media.tumblr.com now and then immediately fallback to the detected
        /// host and use the _1280 file dimension which should be valid in most cases.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public string TestTumblrRawUrl(string url, AppSettings settings)
        {
            if (url.Contains("68.media.tumblr.com") && url.Contains("_raw"))
            {
                return url.Replace("68.media.tumblr.com", "media.tumblr.com");
            }
            if (url.Contains("_raw"))
            {
                return url.Replace("_raw", "_1280");
            }
            return url;
        }

        //public async Task<string> TestRawUrl(string url, AppSettings settings)
        //{
        //    if (!url.Contains("_raw"))
        //        return url;
        //    string path = new Uri(url).LocalPath.TrimStart('/');

        //    foreach (string host in settings.TumblrHosts)
        //    {
        //        string rawUrl = "https://" + host + "/" + path;
        //        if (await UrlExists(rawUrl, settings))
        //            return rawUrl;
        //    }

        //    foreach (string size in settings.ImageSizes)
        //    {
        //        string rawUrl = url.Replace(settings.ImageSize, size);
        //        if (await UrlExists(rawUrl, settings))
        //            return rawUrl;
        //    }

        //    return url;
        //}

        //private async Task<bool> UrlExists(string url, AppSettings settings)
        //{
        //    HttpWebRequest request = CreateWebReqeust(url, settings);
        //    request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        //    request.Method = "HEAD";

        //    try
        //    {
        //        using (var response = (HttpWebResponse)await request.GetResponseAsync())
        //        {
        //            return response.StatusCode == HttpStatusCode.OK;
        //        }
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        private void AddPhotoUrlToDownloadList(string document, IList<string> tags)
        {
            if (blog.DownloadPhoto)
            {
                var regex = new Regex("\"(http[\\S]*media.tumblr.com[\\S]*(jpg|png|gif))\"");
                foreach (Match match in regex.Matches(document))
                {
                    string imageUrl = match.Groups[1].Value;
                    if (imageUrl.Contains("avatar") || imageUrl.Contains("previews"))
                        continue;                    
                    if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                    {
                        continue;
                    }
                    imageUrl = ResizeTumblrImageUrl(imageUrl);
                    // TODO: postID
                    AddToDownloadList(new TumblrPost(PostTypes.Photo, imageUrl, Guid.NewGuid().ToString("N")));
                }
            }
        }

        private void AddVideoUrlToDownloadList(string document, IList<string> tags)
        {
            if (blog.DownloadVideo)
            {
                var regex = new Regex("\"(http[\\S]*.com/video_file/[\\S]*)\"");
                foreach (Match match in regex.Matches(document))
                {
                    string videoUrl = match.Groups[1].Value;
                    // TODO: postId
                    if (shellService.Settings.VideoSize == 1080)
                    {
                        // TODO: postID
                        AddToDownloadList(new TumblrPost(PostTypes.Video, videoUrl.Replace("/480", "") + ".mp4", Guid.NewGuid().ToString("N")));
                    }
                    else if (shellService.Settings.VideoSize == 480)
                    {
                        // TODO: postID
                        AddToDownloadList(new TumblrPost(PostTypes.Video, 
                            "https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + "_480.mp4",
                            Guid.NewGuid().ToString("N")));
                    }
                }
            }
        }

        private void UpdateBlogStats()
        {
            blog.TotalCount = statisticsBag.Count;
            blog.Photos = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Photo));
            blog.Videos = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Video));
            blog.Audios = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Audio));
            blog.Texts = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Text));
            blog.Conversations = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Conversation));
            blog.Quotes = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Quote));
            blog.NumberOfLinks = statisticsBag.Count(url => url.PostType.Equals(PostTypes.Link));
            blog.PhotoMetas = statisticsBag.Count(url => url.PostType.Equals(PostTypes.PhotoMeta));
            blog.VideoMetas = statisticsBag.Count(url => url.PostType.Equals(PostTypes.VideoMeta));
            blog.AudioMetas = statisticsBag.Count(url => url.PostType.Equals(PostTypes.AudioMeta));
        }

        private void AddToDownloadList(TumblrPost addToList)
        {
            producerConsumerCollection.Add(addToList);
            statisticsBag.Add(addToList);
        }
    }
}
