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

using HtmlAgilityPack;

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

        private string GetApiUrl(string url, int count, int start = 0)
        {
            if (url.Last<char>() != '/')
            {
                url += "/api/read";
            }
            else
            {
                url += "api/read";
            }

            var parameters = new Dictionary<string, string>
            {
                { "num", count.ToString() }
            };
            if (start > 0)
            {
                parameters["start"] = start.ToString();
            }
            return url + "?" + UrlEncode(parameters);
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
            //FIXME: IndexOutOfRangeException
            //if (!url.Contains("inline"))
            //    return url.Split('_')[0] + "_" + url.Split('_')[1];
            //else
            //    return url;
            return url;
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

        protected async Task<int> GetTimeOfFirstPostAsync()
        {
            var archiveDoc = new HtmlDocument();
            archiveDoc.LoadHtml(await RequestDataAsync(blog.Url + "archive/"));

            // determine the timespan of the blog to parallelize the search
            // gets the month and year of the first post of the blog
            HtmlNode months_widget = archiveDoc.DocumentNode
                                               .Descendants("div")
                                               .FirstOrDefault(n => n.Attributes["id"]?.Value == "browse_months_widget");
            if (months_widget != null)
            {
                HtmlNode firstYearSection = months_widget.Descendants("section").LastOrDefault();

                int firstMonth =
                    int.Parse(
                        firstYearSection.Element("nav").Descendants("a").FirstOrDefault().Attributes["href"].Value.Split('/')
                                                                                                            .Last());
                int firstYear = int.Parse(firstYearSection.Attributes["id"].Value.Split('_').Last());

                // subtract a month to make sure to get all posts
                if (firstMonth == 1)
                {
                    firstYear--;
                    firstMonth = 12;
                }
                else
                {
                    firstMonth--;
                }

                // determine timespan of the blog or from the last complete crawl to now.
                if (blog.LastCompleteCrawl == DateTime.MinValue || blog.ForceRescan)
                {
                    blog.ForceRescan = false;
                    var dateTimeOfFirstPost = new DateTime(firstYear, firstMonth, 01, 0, 0, 0, DateTimeKind.Utc);
                    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    return (int)(dateTimeOfFirstPost.ToUniversalTime() - epoch).TotalSeconds;
                }
                else
                {
                    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    return (int)(blog.LastCompleteCrawl.ToUniversalTime() - epoch).TotalSeconds;
                }
            }
            return (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        private async Task GetUrlsAsync(IProgress<DownloadProgress> progress, CancellationToken ct, PauseToken pt)
        {
            var semaphoreSlim = new SemaphoreSlim(shellService.Settings.ParallelScans);
            var trackedTasks = new List<Task>();

            var unixTimeNow = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            int timeDifferencePerCrawler = (unixTimeNow - await GetTimeOfFirstPostAsync()) / shellService.Settings.ParallelScans;

            foreach (int crawlerNumber in Enumerable.Range(0, shellService.Settings.ParallelScans))
            {
                await semaphoreSlim.WaitAsync();

                trackedTasks.Add(new Func<Task>(async () =>
                {
                    try
                    {
                        var document = new HtmlDocument();
                        string archiveTime = (unixTimeNow - crawlerNumber * timeDifferencePerCrawler).ToString();
                        string archiveTimeOfPrevCrawler =
                            (unixTimeNow - ((crawlerNumber + 1) * timeDifferencePerCrawler)).ToString();

                        document.LoadHtml(await RequestDataAsync(blog.Url + "archive?before_time=" + archiveTime));

                        await AddUrlsToDownloadList(document, progress, archiveTimeOfPrevCrawler, ct, pt);
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

        private async Task AddUrlsToDownloadList(HtmlDocument document, IProgress<DownloadProgress> progress,
            string archiveTimeOfPrevCrawler, CancellationToken ct, PauseToken pt)
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

            string archiveTime;
            try
            {
                archiveTime =
                    document.DocumentNode.SelectSingleNode("//a[@id='next_page_link']").Attributes["href"].Value.Replace(
                        "/archive?before_time=", "");
                if (int.Parse(archiveTime) < int.Parse(archiveTimeOfPrevCrawler))
                {
                    return;
                }
            }
            catch
            {
                return;
            }
            Interlocked.Increment(ref numberOfPagesCrawled);
            UpdateProgressQueueInformation(progress, Resources.ProgressGetUrlShort, numberOfPagesCrawled);
            document = new HtmlDocument();
            document.LoadHtml(await RequestDataAsync(blog.Url + "archive?before_time=" + archiveTime));
            await AddUrlsToDownloadList(document, progress, archiveTimeOfPrevCrawler, ct, pt);
        }

        private void AddPhotoUrlToDownloadList(HtmlDocument document, IList<string> tags)
        {
            if (blog.DownloadPhoto)
            {
                foreach (HtmlNode post in document.DocumentNode.Descendants("div"))
                {
                    if (post.GetAttributeValue("class", "").Contains("is_photo"))
                    {
                        AddPhotoUrl(post);
                    }
                }
            }
        }

        private void AddVideoUrlToDownloadList(HtmlDocument document, IList<string> tags)
        {
            if (blog.DownloadVideo)
            {
                foreach (HtmlNode post in document.DocumentNode.Descendants("div"))
                {
                    if (post.GetAttributeValue("class", "").Contains("is_video"))
                    {
                        AddVideoUrl(post);
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

        private string ParseImageUrl(HtmlNode post)
        {
            string imageUrl = post.Descendants("div").Where(n => n.GetAttributeValue("class", "")
                                                                  .Contains("has_imageurl"))
                                  .Select(n => n.GetAttributeValue("data-imageurl", ""))
                                  .FirstOrDefault().ToString();

            imageUrl = ResizeTumblrImageUrl(imageUrl);

            return imageUrl;
        }

        private void AddInlinePhotoUrl(XElement post)
        {
            if (!string.Concat(post.Nodes()).Contains("tumblr_inline"))
            {
                return;
            }
            var regex = new Regex("<img src=\"(.*?)\"");
            foreach (Match match in regex.Matches(post.Value))
            {
                string imageUrl = match.Groups[1].Value;
                if (blog.ForceSize)
                {
                    imageUrl = ResizeTumblrImageUrl(imageUrl);
                }
                AddToDownloadList(Tuple.Create(PostTypes.Photo, imageUrl, post.Attribute("id").Value));
            }
        }

        private void AddPhotoUrl(HtmlNode post)
        {
            string imageUrl = ParseImageUrl(post);
            if (blog.SkipGif && imageUrl.EndsWith(".gif"))
            {
                return;
            }
            AddToDownloadList(Tuple.Create(PostTypes.Photo, imageUrl, post.Attributes["id"].Value.Split('_').Last()));
        }

        private void AddPhotoSetUrl(XElement post)
        {
            //if (!post.Descendants("photoset").Any())
            //{
            //    return;
            //}
            //foreach (string imageUrl in post.Descendants("photoset")
            //    .Descendants("photo")
            //    .Select(ParseImageUrl)
            //    .Where(imageUrl => !blog.SkipGif || !imageUrl.EndsWith(".gif")))
            //{
            //    AddToDownloadList(Tuple.Create(PostTypes.Photo, imageUrl, post.Attribute("id").Value));
            //}
        }

        private void AddVideoUrl(HtmlNode post)
        {
            string videoUrl =
                post.Descendants("div")
                    .Where(n => n.GetAttributeValue("class", "").Contains("has_imageurl"))
                    .Select(n => n.GetAttributeValue("data-imageurl", ""))
                    .FirstOrDefault();
            videoUrl = videoUrl.Split('/').Last();
            string postId = post.Attributes["id"].Value.Split('_').Last();

            if (shellService.Settings.VideoSize == 1080)
            {
                videoUrl = "https://vt.tumblr.com/" + videoUrl.Split('_')[0] + "_" + videoUrl.Split('_')[1] + ".mp4";
                AddToDownloadList(Tuple.Create(PostTypes.Video, videoUrl, postId));
            }
            else if (shellService.Settings.VideoSize == 480)
            {
                videoUrl = "https://vt.tumblr.com/" + videoUrl.Split('_')[0] + "_" + videoUrl.Split('_')[1] + "_480.mp4";
                AddToDownloadList(Tuple.Create(PostTypes.Video, videoUrl, postId));
            }
        }

        private void AddAudioUrl(XElement post)
        {
            string audioUrl = post.Descendants("audio-player")
                                  .Where(x => x.Value.Contains("src="))
                                  .Select(result => Regex.Match(result.Value, "src=\"(.*)\" height")
                                                         .Groups[1].Value)
                                  .FirstOrDefault();

            AddToDownloadList(Tuple.Create(PostTypes.Audio, WebUtility.UrlDecode(audioUrl), post.Attribute("id").Value));
        }

        private static string ParsePhotoMeta(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoUrl, post.Element("photo-url").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoCaption, post.Element("photo-caption")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseVideoMeta(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.VideoPlayer, post.Element("video-player")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseAudioMeta(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.AudioCaption, post.Element("audio-caption")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Artist, post.Element("id3-artist")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Title, post.Element("id3-title")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.Element("id3-track")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Album, post.Element("id3-album")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Year, post.Element("id3-year")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseConversation(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Conversation, post.Element("conversation-text")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseLink(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Link, post.Element("link-text")?.Value) +
                   Environment.NewLine + post.Element("link-url")?.Value +
                   Environment.NewLine + post.Element("link-description")?.Value +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseQuote(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.Element("quote-text")?.Value) +
                   Environment.NewLine + post.Element("quote-source")?.Value +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseText(XElement post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Attribute("id").Value) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.Attribute("date-gmt").Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.Attribute("url-with-slug")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.Attribute("reblog-key")?.Value) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Title, post.Element("regular-title")?.Value) +
                   Environment.NewLine + post.Element("regular-body")?.Value +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Elements("tag")?.Select(x => x.Value).ToArray())) +
                   Environment.NewLine;
        }
    }
}
