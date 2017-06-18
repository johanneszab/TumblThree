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

using TumblThree.Applications.DataModels;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Downloader
{
    [Export(typeof(IDownloader))]
    [ExportMetadata("BlogType", BlogTypes.tumblr)]
    public class TumblrDownloader : Downloader, IDownloader
    {
        private readonly IBlog blog;
        private readonly ICrawlerService crawlerService;
        private readonly IShellService shellService;
        private int numberOfPagesCrawled = 0;

        public TumblrDownloader(IShellService shellService, ICrawlerService crawlerService, IBlog blog)
            : base(shellService, crawlerService, blog)
        {
            this.shellService = shellService;
            this.crawlerService = crawlerService;
            this.blog = blog;
        }

        public async Task Crawl(IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            Logger.Verbose("TumblrDownloader.Crawl:Start");

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

        private IEnumerable<int> GetPageNumbers()
        {
            if (string.IsNullOrEmpty(blog.DownloadPages))
            {
                int totalPosts = blog.Posts;
                int totalPages = (totalPosts / blog.PageSize) + 1;

                return Enumerable.Range(0, totalPages);
            }
            return RangeToSequence(blog.DownloadPages);
        }

        private static bool TestRange(int numberToCheck, int bottom, int top)
        {
            return (numberToCheck >= bottom && numberToCheck <= top);
        }

        static IEnumerable<int> RangeToSequence(string input)
        {
            string[] parts = input.Split(',');
            foreach (string part in parts)
            {
                if (!part.Contains('-'))
                {
                    yield return int.Parse(part);
                    continue;
                }
                string[] rangeParts = part.Split('-');
                int start = int.Parse(rangeParts[0]);
                int end = int.Parse(rangeParts[1]);

                while (start <= end)
                {
                    yield return start;
                    start++;
                }
            }
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
                    var tags = new List<string>();
                    if (!string.IsNullOrWhiteSpace(blog.Tags))
                    {
                        tags = blog.Tags.Split(',').Select(x => x.Trim()).ToList();
                    }

                    try
                    {
                        string document = await RequestDataAsync(blog.Url + "page/" + crawlerNumber);
                        await AddUrlsToDownloadList(document, tags, progress, crawlerNumber, ct, pt);
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

        private async Task AddUrlsToDownloadList(string document, IList<string> tags, IProgress<DownloadProgress> progress,
            int crawlerNumber, CancellationToken ct, PauseToken pt)
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }
            if (pt.IsPaused)
            {
                pt.WaitWhilePausedWithResponseAsyc().Wait();
            }

            AddPhotoUrlToDownloadList(document, tags);
            AddVideoUrlToDownloadList(document, tags);

            Interlocked.Increment(ref numberOfPagesCrawled);
            UpdateProgressQueueInformation(progress, Resources.ProgressGetUrlShort, numberOfPagesCrawled);
            document = await RequestDataAsync(blog.Url + "page/" + crawlerNumber);
            if (!document.Contains((crawlerNumber + 1).ToString()))
                return;
            crawlerNumber += shellService.Settings.ParallelScans;
            await AddUrlsToDownloadList(document, tags, progress, crawlerNumber, ct, pt);
        }

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
                    // FIXME: postID
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
                    string videoUrl = match.Groups[0].Value;
                    if (shellService.Settings.VideoSize == 1080)
                    {
                        // FIXME: postID
                        AddToDownloadList(new TumblrPost(PostTypes.Video,
                            "https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + ".mp4",
                            Guid.NewGuid().ToString("N")));
                    }
                    else if (shellService.Settings.VideoSize == 480)
                    {
                        // FIXME: postID
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
            if (statisticsBag.All(download => download.Url != addToList.Url))
            {
                producerConsumerCollection.Add(addToList);
            }
            statisticsBag.Add(addToList);
        }
    }
}
