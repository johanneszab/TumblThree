using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;

using Post = TumblThree.Applications.DataModels.Post;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", BlogTypes.tumblr)]
    public class TumblrBlogCrawler : AbstractCrawler, ICrawler
    {
        public TumblrBlogCrawler(IShellService shellService, CancellationToken ct, PauseToken pt,
            IProgress<DownloadProgress> progress, ICrawlerService crawlerService, IDownloader downloader, BlockingCollection<TumblrPost> producerConsumerCollection, IBlog blog, IFiles files)
            : base(shellService, ct, pt, progress, crawlerService, downloader, producerConsumerCollection, blog, files)
        {
        }

        public override async Task IsBlogOnlineAsync()
        {
            try
            {
                string document = await GetSvcPageAsync("1", "0");
                blog.Online = true;
            }
            catch (WebException webException)
            {
                if (webException.Message.Contains("429"))
                {
                    Logger.Error("TumblrPrivateBlogCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                    shellService.ShowError(webException, Resources.LimitExceeded, blog.Name);
                    blog.Online = true;
                    return;
                }
                blog.Online = false;
            }
        }

        public override async Task UpdateMetaInformationAsync()
        {
            try
            {
                if (blog.Online)
                {
                    string document = await GetSvcPageAsync("1", "0");
                    var response = ConvertJsonToClass<TumblrJson>(document);

                    if (response.meta.status == 200)
                    {
                        blog.Title = response.response.posts.FirstOrDefault().blog.title;
                        blog.Description = response.response.posts.FirstOrDefault().blog.description;
                    }
                }
            }
            catch (WebException webException)
            {
                var webRespStatusCode = (int)((HttpWebResponse)webException?.Response).StatusCode;
                if (webRespStatusCode == 503)
                {
                    Logger.Error("TumblrPrivateCrawler:GetUrlsAsync: {0}", "User not logged in");
                    shellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, blog.Name);
                }
                else
                {
                    blog.Online = false;
                }
            }
        }

        public async Task Crawl()
        {
            Logger.Verbose("TumblrPrivateCrawler.Crawl:Start");

            Task grabber = GetUrlsAsync();
            Task<bool> download = downloader.DownloadBlogAsync();

            await grabber;

            UpdateProgressQueueInformation(Resources.ProgressUniqueDownloads);
            blog.DuplicatePhotos = DetermineDuplicates(PostTypes.Photo);
            blog.DuplicateVideos = DetermineDuplicates(PostTypes.Video);
            blog.DuplicateAudios = DetermineDuplicates(PostTypes.Audio);
            blog.TotalCount = (blog.TotalCount - blog.DuplicatePhotos - blog.DuplicateAudios - blog.DuplicateVideos);

            CleanCollectedBlogStatistics();

            await download;

            if (!ct.IsCancellationRequested)
            {
                blog.LastCompleteCrawl = DateTime.Now;
            }

            blog.Save();

            UpdateProgressQueueInformation("");
        }

        protected override IEnumerable<int> GetPageNumbers()
        {
            if (!TestRange(blog.PageSize, 1, 100))
                blog.PageSize = 100;

            if (string.IsNullOrEmpty(blog.DownloadPages))
            {
                return Enumerable.Range(0, shellService.Settings.ParallelScans);
            }
            return RangeToSequence(blog.DownloadPages);
        }

        private async Task<ulong> GetHighestPostId()
        {
            string document = await GetSvcPageAsync("1", "0");
            var response = ConvertJsonToClass<TumblrJson>(document);

            ulong highestId;
            ulong.TryParse(blog.Title = response.response.posts.FirstOrDefault().id, out highestId);
            return highestId;
        }

        private static bool CheckPostAge(TumblrJson document, ulong lastId)
        {
            ulong highestPostId = 0;
            ulong.TryParse(document.response.posts.FirstOrDefault().id,
                out highestPostId);

            if (highestPostId < lastId)
            {
                return false;
            }
            return true;
        }

        private async Task GetUrlsAsync()
        {
            var semaphoreSlim = new SemaphoreSlim(shellService.Settings.ParallelScans);
            var trackedTasks = new List<Task>();

            if (!await CheckIfLoggedIn())
            {
                Logger.Error("TumblrPrivateCrawler:GetUrlsAsync: {0}", "User not logged in");
                shellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, blog.Name);
                producerConsumerCollection.CompleteAdding();
                return;
            }

            foreach (int crawlerNumber in Enumerable.Range(0, shellService.Settings.ParallelScans))
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
                        string document = await GetSvcPageAsync(blog.PageSize.ToString(), (blog.PageSize * crawlerNumber).ToString());
                        var response = ConvertJsonToClass<TumblrJson>(document);
                        await AddUrlsToDownloadList(response, crawlerNumber);
                    }
                    catch (WebException webException)
                    {
                        if (webException.Message.Contains("429"))
                        {
                            // TODO: add retry logic?
                            Logger.Error("TumblrPrivateCrawler:GetUrls:WebException {0}", webException);
                            shellService.ShowError(webException, Resources.LimitExceeded, blog.Name);
                        }
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

            //if (!ct.IsCancellationRequested)
            //{
            UpdateBlogStats();
            //}
        }

        private async Task<bool> CheckIfLoggedIn()
        {
            try
            {
                string document = await GetSvcPageAsync(blog.PageSize.ToString(), (blog.PageSize * 1).ToString());
            }
            catch (WebException webException)
            {
                if (webException.Message.Contains("503"))
                {
                    return false;
                }
            }
            return true;
        }

        private async Task<string> GetSvcPageAsync(string limit, string offset)
        {
            if (shellService.Settings.LimitConnections)
            {
                crawlerService.Timeconstraint.Acquire();
                return await RequestDataAsync(limit, offset);
            }
            return await RequestDataAsync(limit, offset);
        }

        protected virtual async Task<string> RequestDataAsync(string limit, string offset)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                string url = @"https://www.tumblr.com/svc/indash_blog?tumblelog_name_or_id=" + blog.Name +
                    @"&post_id=&limit=" + limit + "&offset=" + offset + "&should_bypass_safemode=true";
                string referer = @"https://www.tumblr.com/dashboard/blog/" + blog.Name;
                HttpWebRequest request = CreateGetXhrReqeust(url, referer);
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

        private async Task AddUrlsToDownloadList(TumblrJson response, int crawlerNumber)
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

                try
                {
                    AddPhotoUrlToDownloadList(response);
                    AddVideoUrlToDownloadList(response);
                    AddAudioUrlToDownloadList(response);
                    AddTextUrlToDownloadList(response);
                    AddQuoteUrlToDownloadList(response);
                    AddLinkUrlToDownloadList(response);
                    AddConversationUrlToDownloadList(response);
                    AddAnswerUrlToDownloadList(response);
                    AddPhotoMetaUrlToDownloadList(response);
                    AddVideoMetaUrlToDownloadList(response);
                    AddAudioMetaUrlToDownloadList(response);
                }
                catch (NullReferenceException)
                {
                }

                Interlocked.Increment(ref numberOfPagesCrawled);
                UpdateProgressQueueInformation(Resources.ProgressGetUrlShort, numberOfPagesCrawled);

                string document = await GetSvcPageAsync(blog.PageSize.ToString(), (blog.PageSize * crawlerNumber).ToString());
                response = ConvertJsonToClass<TumblrJson>(document);
                if (!response.response.posts.Any())
                {
                    return;
                }

                crawlerNumber += shellService.Settings.ParallelScans;
            }
        }

        private bool CheckIfDownloadRebloggedPosts(Post post)
        {
            if (!blog.DownloadRebloggedPosts)
            {
                if (!post.reblogged_from_url.Any())
                    return true;
                return false;
            }
            return true;
        }

        private void AddPhotoUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadPhoto)
            {
                foreach (Post post in document.response.posts)
                {
                    if (post.type == "photo" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            AddPhotoUrl(post);
                        }
                    }
                    // check for inline images
                    if (post.type != "photo" && !tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any())
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                            try { AddInlinePhotoUrl(post); }
                            catch { }
                    }
                }
            }
        }

        private void AddPhotoUrl(Post post)
        {
            string postId = post.id;
            foreach (Photo photo in post.photos)
            {
                string imageUrl = photo.alt_sizes.Where(url => url.width == int.Parse(ImageSize())).Select(url => url.url).FirstOrDefault();
                if (imageUrl == null)
                    imageUrl = photo.alt_sizes.FirstOrDefault().url;

                if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                {
                    continue;
                }

                AddToDownloadList(new TumblrPost(PostTypes.Photo, imageUrl, postId, post.timestamp.ToString()));
            }
        }

        private void AddInlinePhotoUrl(Post post)
        {
            var regex = new Regex("\"(http[A-Za-z0-9_/:.]*media.tumblr.com[A-Za-z0-9_/:.]*(jpg|png|gif))\"");
            foreach (Match match in regex.Matches(post.body))
            {
                string postId = post.id;

                string imageUrl = match.Groups[1].Value;
                if (imageUrl.Contains("avatar") || imageUrl.Contains("previews"))
                    continue;
                if (blog.SkipGif && imageUrl.EndsWith(".gif"))
                {
                    continue;
                }
                AddToDownloadList(new TumblrPost(PostTypes.Photo, imageUrl, postId, post.timestamp.ToString()));
            }
        }


        private void AddVideoUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadVideo)
            {
                foreach (Post post in document.response.posts)
                {
                    if (post.type == "video" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            AddVideoUrl(post);
                        }
                    }
                    // check for inline videos
                    if (post.type != "video" && !tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any())
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                            try { AddInlineVideoUrl(post); }
                            catch { }
                    }
                }
            }
        }

        private void AddVideoUrl(Post post)
        {
            string postId = post.id;
            string videoUrl = post.video_url;

            if (shellService.Settings.VideoSize == 480)
            {
                if (!videoUrl.Contains("_480"))
                {
                    videoUrl.Replace(".mp4", "_480.mp4");
                }
            }
            AddToDownloadList(new TumblrPost(PostTypes.Video, videoUrl, postId, post.timestamp.ToString()));
        }

        private void AddInlineVideoUrl(Post post)
        {
            var regex = new Regex("\"(http[A-Za-z0-9_/:.]*.com/video_file/[A-Za-z0-9_/:.]*)\"");
            foreach (Match match in regex.Matches(post.body))
            {
                string videoUrl = match.Groups[1].Value;
                if (shellService.Settings.VideoSize == 1080)
                {
                    AddToDownloadList(new TumblrPost(PostTypes.Video, videoUrl.Replace("/480", "") + ".mp4", post.id, post.timestamp.ToString()));
                }
                else if (shellService.Settings.VideoSize == 480)
                {
                    AddToDownloadList(new TumblrPost(PostTypes.Video,
                        "https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + "_480.mp4",
                        post.id, post.timestamp.ToString()));
                }
            }
        }

        private void AddAudioUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadAudio)
            {
                foreach (Post post in document.response.posts)
                {
                    if (post.type == "audio" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string audioUrl = post.audio_url;
                            if (!audioUrl.EndsWith(".mp3"))
                                audioUrl = audioUrl + ".mp3";
                            AddToDownloadList(new TumblrPost(PostTypes.Audio, audioUrl, postId, post.timestamp.ToString()));
                        }
                    }
                }
            }
        }

        private void AddTextUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadText)
            {
                foreach (Post post in document.response.posts)
                {
                    if (post.type == "text" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParseText(post);
                            AddToDownloadList(new TumblrPost(PostTypes.Text, textBody, postId, post.timestamp.ToString()));
                        }
                    }
                }
            }
        }

        private void AddQuoteUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadQuote)
            {
                foreach (Post post in document.response.posts)
                {
                    if (post.type == "quote" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParseQuote(post);
                            AddToDownloadList(new TumblrPost(PostTypes.Quote, textBody, postId, post.timestamp.ToString()));
                        }
                    }
                }
            }
        }

        private void AddLinkUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadLink)
            {
                foreach (Post post in document.response.posts)
                {
                    if (post.type == "link" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParseLink(post);
                            AddToDownloadList(new TumblrPost(PostTypes.Link, textBody, postId, post.timestamp.ToString()));
                        }
                    }
                }
            }
        }

        private void AddConversationUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadConversation)
            {
                foreach (Post post in document.response.posts)
                {
                    if (post.type == "chat" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParseConversation(post);
                            AddToDownloadList(new TumblrPost(PostTypes.Conversation, textBody, postId, post.timestamp.ToString()));
                        }
                    }
                }
            }
        }

        private void AddAnswerUrlToDownloadList(TumblrJson document)
        {
            if (blog.DownloadAnswer)
            {
                foreach (Post post in document.response.posts)
                {
                    if (post.type == "answer" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParseAnswer(post);
                            AddToDownloadList(new TumblrPost(PostTypes.Answer, textBody, postId, post.timestamp.ToString()));
                        }
                    }
                }
            }
        }

        private void AddPhotoMetaUrlToDownloadList(TumblrJson document)
        {
            if (blog.CreatePhotoMeta)
            {
                foreach (Post post in document.response.posts)
                {
                    if (post.type == "photo" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParsePhotoMeta(post);
                            AddToDownloadList(new TumblrPost(PostTypes.PhotoMeta, textBody, postId));
                        }
                    }
                }
            }
        }

        private void AddVideoMetaUrlToDownloadList(TumblrJson document)
        {
            if (blog.CreateVideoMeta)
            {
                foreach (Post post in document.response.posts)
                {
                    if (post.type == "video" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParseVideoMeta(post);
                            AddToDownloadList(new TumblrPost(PostTypes.VideoMeta, textBody, postId));
                        }
                    }
                }
            }
        }

        private void AddAudioMetaUrlToDownloadList(TumblrJson document)
        {
            if (blog.CreateAudioMeta)
            {
                foreach (Post post in document.response.posts)
                {
                    if (post.type == "audio" && (!tags.Any() || post.tags.Intersect(tags, StringComparer.OrdinalIgnoreCase).Any()))
                    {
                        if (CheckIfDownloadRebloggedPosts(post))
                        {
                            string postId = post.id;
                            string textBody = ParseAudioMeta(post);
                            AddToDownloadList(new TumblrPost(PostTypes.AudioMeta, textBody, postId));
                        }
                    }
                }
            }
        }

        private static string ParseText(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Title, post.title) +
                   Environment.NewLine + post.body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseQuote(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.dialogue.Select(dialogue => dialogue.phrase).FirstOrDefault()) +
                   Environment.NewLine + post.body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseLink(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Link, post.link_url) +
                   Environment.NewLine + post.body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseConversation(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.dialogue.Select(dialogue => dialogue.phrase).FirstOrDefault()) +
                   Environment.NewLine + post.body +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseAnswer(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   post.question +
                   Environment.NewLine +
                   post.answer +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        private static string ParsePhotoMeta(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoUrl, post.photos.Select(photo => photo.original_size.url).FirstOrDefault()) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoCaption, post.photos.Select(photo => photo.caption).FirstOrDefault()) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseVideoMeta(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.VideoPlayer, post.caption) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }

        private static string ParseAudioMeta(Post post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.id) + ", " +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.date) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.UrlWithSlug, post.slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.reblog_key) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.reblogged_from_url) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.reblogged_from_name) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.AudioCaption, post.caption) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Artist, post.artist) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Title, post.title) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.track) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Album, post.album) +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.year) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.tags.ToArray())) +
                   Environment.NewLine;
        }
    }
}
