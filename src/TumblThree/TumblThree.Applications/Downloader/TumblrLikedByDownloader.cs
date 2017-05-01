using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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
                .Replace("_1280", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_540", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_500", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_400", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_250", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_100", "_" + shellService.Settings.ImageSize.ToString())
                .Replace("_75sq", "_" + shellService.Settings.ImageSize.ToString())
                .ToString();
        }

        /// <returns>
        ///     Return the url without the size and type suffix (e.g.
        ///     https://68.media.tumblr.com/51a99943f4aa7068b6fd9a6b36e4961b/tumblr_mnj6m9Huml1qat3lvo1).
        /// </returns>
        protected override string GetCoreImageUrl(string url)
        {
            return url.Split('_')[0] + "_" + url.Split('_')[1];
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
            return statisticsBag.Where(url => url.Item1.Equals(type))
                                .GroupBy(url => url.Item2)
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
                return;
            await AddUrlsToDownloadList(document, progress, crawlerNumber, ct, pt);
        }

        private void AddPhotoUrlToDownloadList(string document, IList<string> tags)
        {
            if (blog.DownloadPhoto)
            {
                var regex = new Regex("data-big-photo=\"(.*)\" ");
                foreach (Match match in regex.Matches(document))
                {
                    string imageUrl = match.Groups[1].Value;
                    if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                    {
                        continue;
                    }
                    imageUrl = ResizeTumblrImageUrl(imageUrl);
                    // FIXME: postID
                    AddToDownloadList(Tuple.Create(PostTypes.Photo, imageUrl, Guid.NewGuid().ToString("N")));
                }
            }
        }

        private void AddVideoUrlToDownloadList(string document, IList<string> tags)
        {
            if (blog.DownloadVideo)
            {
                var regex = new Regex("<source src=\"(.*)\" type=\"video/mp4\"");
                foreach (Match match in regex.Matches(document))
                {
                    string videoUrl = match.Groups[1].Value;
                    // FIXME: postId
                    if (shellService.Settings.VideoSize == 1080)
                    {
                        // FIXME: postID
                        AddToDownloadList(Tuple.Create(PostTypes.Video, videoUrl.Replace("/480", "") + ".mp4", Guid.NewGuid().ToString("N")));
                    }
                    else if (shellService.Settings.VideoSize == 480)
                    {
                        // FIXME: postID
                        AddToDownloadList(Tuple.Create(PostTypes.Video, 
                            "https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + "_480.mp4",
                            Guid.NewGuid().ToString("N")));
                    }
                }
            }
        }

        private void UpdateBlogStats()
        {
            blog.TotalCount = statisticsBag.Count;
            blog.Photos = statisticsBag.Count(url => url.Item1.Equals(PostTypes.Photo));
            blog.Videos = statisticsBag.Count(url => url.Item1.Equals(PostTypes.Video));
            blog.Audios = statisticsBag.Count(url => url.Item1.Equals(PostTypes.Audio));
            blog.Texts = statisticsBag.Count(url => url.Item1.Equals(PostTypes.Text));
            blog.Conversations = statisticsBag.Count(url => url.Item1.Equals(PostTypes.Conversation));
            blog.Quotes = statisticsBag.Count(url => url.Item1.Equals(PostTypes.Quote));
            blog.NumberOfLinks = statisticsBag.Count(url => url.Item1.Equals(PostTypes.Link));
            blog.PhotoMetas = statisticsBag.Count(url => url.Item1.Equals(PostTypes.PhotoMeta));
            blog.VideoMetas = statisticsBag.Count(url => url.Item1.Equals(PostTypes.VideoMeta));
            blog.AudioMetas = statisticsBag.Count(url => url.Item1.Equals(PostTypes.AudioMeta));
        }

        private void AddToDownloadList(Tuple<PostTypes, string, string> addToList)
        {
            if (statisticsBag.All(download => download.Item2 != addToList.Item2))
            {
                statisticsBag.Add(addToList);
                producerConsumerCollection.Add(addToList);
            }
        }
    }
}
