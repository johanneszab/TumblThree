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
using TumblThree.Applications.DataModels.TumblrSearchJson;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Downloader
{
    [Export(typeof(IDownloader))]
    [ExportMetadata("BlogType", BlogTypes.tumblrsearch)]
    public class TumblrSearchDownloader : TumblrDownloader, IDownloader
    {
        private string tumblrKey = String.Empty;

        public TumblrSearchDownloader(IShellService shellService, CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress, PostCounter counter, FileDownloader fileDownloader, ICrawlerService crawlerService, IBlog blog, IFiles files)
            : base(shellService, ct, pt, progress, counter, fileDownloader, crawlerService, blog, files)
        {
        }

        public async Task Crawl()
        {
            Logger.Verbose("TumblrSearchDownloader.Crawl:Start");

            Task grabber = GetUrlsAsync();
            Task<bool> downloader = DownloadBlogAsync();

            await grabber;

            UpdateProgressQueueInformation(Resources.ProgressUniqueDownloads);
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

            UpdateProgressQueueInformation("");
        }

        private async Task GetUrlsAsync()
        {
            var semaphoreSlim = new SemaphoreSlim(shellService.Settings.ParallelScans);
            var trackedTasks = new List<Task>();
            await UpdateTumblrKey();

            foreach (int crawlerNumber in Enumerable.Range(1, shellService.Settings.ParallelScans))
            {
                await semaphoreSlim.WaitAsync();

                trackedTasks.Add(new Func<Task>(async () =>
                {
                    if (!string.IsNullOrWhiteSpace(blog.Tags))
                    {
                        tags = blog.Tags.Split(',').Select(x => x.Trim()).ToList();
                    }

                    try
                    {
                        string document = await GetSearchPageAsync(crawlerNumber);
                        await AddUrlsToDownloadList(document, crawlerNumber);
                    }
                    catch
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

        private async Task UpdateTumblrKey()
        {
            string document = await RequestGetAsync();
            tumblrKey = ExtractTumblrKey(document);
        }

        private static string ExtractTumblrKey(string document)
        {
            return Regex.Match(document, "id=\"tumblr_form_key\" content=\"([\\S]*)\">").Groups[1].Value;
        }

        private async Task<string> GetSearchPageAsync(int pageNumber)
        {
            if (shellService.Settings.LimitConnections)
            {
                return await RequestPostAsync(pageNumber);
            }
            return await RequestPostAsync(pageNumber);
        }

        protected virtual async Task<string> RequestPostAsync(int pageNumber)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                string url = "https://www.tumblr.com/search/" + blog.Name + "/post_page/" + pageNumber;
                string referer = @"https://www.tumblr.com/search/" + blog.Name;
                var headers = new Dictionary<string, string>();
                headers.Add("X-tumblr-form-key", tumblrKey);
                headers.Add("DNT", "1");
                HttpWebRequest request = CreatePostReqeust(url, referer, headers);

                //Complete requestBody from webbrowser, searching for cars:
                //q=cars&sort=top&post_view=masonry&blogs_before=8&num_blogs_shown=8&num_posts_shown=20&before=24&blog_page=2&safe_mode=true&post_page=2&filter_nsfw=true&filter_post_type=&next_ad_offset=0&ad_placement_id=0&more_posts=true
                string requestBody = "q=" + blog.Name + "&sort=top&post_view=masonry&num_posts_shown=" + ((pageNumber - 1) * 20) + "&before=" + ((pageNumber - 1) * 20) + "&safe_mode=false&post_page=" + pageNumber + "&filter_nsfw=false&filter_post_type=&next_ad_offset=0&ad_placement_id=0&more_posts=true";
                using (Stream postStream = await request.GetRequestStreamAsync())
                {
                    byte[] postBytes = Encoding.ASCII.GetBytes(requestBody);
                    await postStream.WriteAsync(postBytes, 0, postBytes.Length);
                    await postStream.FlushAsync();
                }

                requestRegistration = ct.Register(() => request.Abort());
                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    using (var stream = GetStreamForApiRequest(response.GetResponseStream()))
                    {
                        using (var buffer = new BufferedStream(stream))
                        {
                            using (var reader = new StreamReader(buffer))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            finally
            {
                requestRegistration.Dispose();
            }
        }

        protected virtual async Task<string> RequestGetAsync()
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                string url = "https://www.tumblr.com/search/" + blog.Name;
                HttpWebRequest request = CreateGetReqeust(url);

                requestRegistration = ct.Register(() => request.Abort());
                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    using (var stream = GetStreamForApiRequest(response.GetResponseStream()))
                    {
                        using (var buffer = new BufferedStream(stream))
                        {
                            using (var reader = new StreamReader(buffer))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            finally
            {
                requestRegistration.Dispose();
            }
        }



        private async Task AddUrlsToDownloadList(string response, int crawlerNumber)
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

                var result = ConvertJsonToClass<TumblrSearchJson>(response);
                if (string.IsNullOrEmpty(result.response.posts_html))
                {
                    return;
                }

                try
                {
                    string html = result.response.posts_html;
                    html = Regex.Unescape(html);
                    AddPhotoUrlToDownloadList(html);
                    AddVideoUrlToDownloadList(html);
                }
                catch (NullReferenceException)
                {
                }

                Interlocked.Increment(ref numberOfPagesCrawled);
                UpdateProgressQueueInformation(Resources.ProgressGetUrlShort, numberOfPagesCrawled);
                response = await GetSearchPageAsync((crawlerNumber + shellService.Settings.ParallelScans));
                crawlerNumber += shellService.Settings.ParallelScans;
            }
        }

        private void AddPhotoUrlToDownloadList(string document)
        {
            if (blog.DownloadPhoto)
            {
                var regex = new Regex("\"(http[A-Za-z0-9_/:.]*media.tumblr.com[A-Za-z0-9_/:.]*(jpg|png|gif))\"");
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

        private void AddVideoUrlToDownloadList(string document)
        {
            if (blog.DownloadVideo)
            {
                var regex = new Regex("\"(http[A-Za-z0-9_/:.]*.com/video_file/[A-Za-z0-9_/:.]*)\"");
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
    }
}
