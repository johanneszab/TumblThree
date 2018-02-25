using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrApiJson;
using TumblThree.Applications.DataModels.TumblrCrawlerData;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Extensions;
using TumblThree.Applications.Parser;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
	[Export(typeof(ICrawler))]
	[ExportMetadata("BlogType", typeof(TumblrBlog))]
	public class TumblrBlogCrawler : AbstractCrawler, ICrawler
	{
		private readonly ICrawlerService crawlerService;
		private readonly IDownloader downloader;
		private readonly PauseToken pt;
		private readonly ITumblrToTextParser<Post> tumblrJsonParser;
		private readonly IImgurParser imgurParser;
		private readonly IGfycatParser gfycatParser;
		private readonly IWebmshareParser webmshareParser;
		private readonly IMixtapeParser mixtapeParser;
		private readonly IMegaParser megaParser;
		private readonly IGoogleDriveParser googledriveParser;
		private readonly IUguuParser uguuParser;
		private readonly ISafeMoeParser safemoeParser;
		private readonly ILoliSafeParser lolisafeParser;
		private readonly ICatBoxParser catboxParser;
		private readonly IPostQueue<TumblrCrawlerData<Post>> jsonQueue;
		private readonly ICrawlerDataDownloader crawlerDataDownloader;

		public TumblrBlogCrawler(IShellService shellService, CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress,
			ICrawlerService crawlerService, IWebRequestFactory webRequestFactory, ISharedCookieService cookieService,
			IDownloader downloader, ICrawlerDataDownloader crawlerDataDownloader, ITumblrToTextParser<Post> tumblrJsonParser, IImgurParser imgurParser, IGfycatParser gfycatParser,
			IWebmshareParser webmshareParser, IMixtapeParser mixtapeParser, IMegaParser megaParser, IGoogleDriveParser googledriveParser, IUguuParser uguuParser,
			ISafeMoeParser safemoeParser, ILoliSafeParser lolisafeParser, ICatBoxParser catboxParser, IPostQueue<TumblrPost> postQueue,
			IPostQueue<TumblrCrawlerData<Post>> jsonQueue, IBlog blog)
			: base(shellService, ct, progress, webRequestFactory, cookieService, postQueue, blog)
		{
			this.crawlerService = crawlerService;
			this.downloader = downloader;
			this.pt = pt;
			this.tumblrJsonParser = tumblrJsonParser;
			this.imgurParser = imgurParser;
			this.gfycatParser = gfycatParser;
			this.webmshareParser = webmshareParser;
			this.mixtapeParser = mixtapeParser;
			this.megaParser = megaParser;
			this.googledriveParser = googledriveParser;
			this.uguuParser = uguuParser;
			this.safemoeParser = safemoeParser;
			this.lolisafeParser = lolisafeParser;
			this.catboxParser = catboxParser;
			this.jsonQueue = jsonQueue;
			this.crawlerDataDownloader = crawlerDataDownloader;
		}

		public override async Task IsBlogOnlineAsync()
		{
			try
			{
				await GetApiPageAsync(0);
				blog.Online = true;
			}
			catch (WebException webException)
			{
				if ((webException.Status == WebExceptionStatus.ProtocolError) && (webException.Response != null))
				{
					HttpWebResponse resp = (HttpWebResponse) webException.Response;
					if (resp.StatusCode == HttpStatusCode.Unauthorized)
					{
						Logger.Error("TumblrBlogCrawler:IsBlogOnlineAsync:WebException {0}", webException);
						shellService.ShowError(webException, Resources.PasswordProtected, blog.Name);
						blog.Online = true;
						return;
					}
				}

				if ((webException.Status == WebExceptionStatus.ProtocolError) && (webException.Response != null))
				{
					HttpWebResponse resp = (HttpWebResponse) webException.Response;
					if ((int) resp.StatusCode == 429)
					{
						Logger.Error("TumblrBlogCrawler:IsBlogOnlineAsync:WebException {0}", webException);
						shellService.ShowError(webException, Resources.LimitExceeded, blog.Name);
						blog.Online = true;
						return;
					}
				}

				Logger.Error("TumblrBlogCrawler:IsBlogOnlineAsync:WebException {0}", webException);
				shellService.ShowError(webException, Resources.BlogIsOffline, blog.Name);
				blog.Online = false;
			}
		}

		public override async Task UpdateMetaInformationAsync()
		{
			try
			{
				await UpdateMetaInformation();
			}
			catch (WebException webException)
			{
				int webRespStatusCode = (int) ((HttpWebResponse) webException.Response).StatusCode;
				if (webRespStatusCode == 429)
				{
					Logger.Error("TumblrBlogCrawler:UpdateMetaInformationAsync:WebException {0}", webException);
					shellService.ShowError(webException, Resources.LimitExceeded, blog.Name);
				}
			}
		}

		private async Task UpdateMetaInformation()
		{
			if (blog.Online)
			{
				string document = await GetApiPageAsync(0);
				TumblrApiJson response = ConvertJsonToClass<TumblrApiJson>(document);

				blog.Title = response.tumblelog?.title;
				blog.Description = response.tumblelog?.description;
				blog.TotalCount = response.posts_total;
			}
		}

		public new static T ConvertJsonToClass<T>(string json) where T : new()
		{
			try
			{
				using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
				{
					DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
					return (T) serializer.ReadObject(ms);
				}
			}
			catch (System.Runtime.Serialization.SerializationException)
			{
				return new T();
			}
		}

		public async Task Crawl()
		{
			Logger.Verbose("TumblrBlogCrawler.Crawl:Start");

			Task<Tuple<ulong, bool>> grabber = GetUrlsAsync();

			// FIXME: refactor downloader out of class
			Task<bool> download = downloader.DownloadBlogAsync();

			Task crawlerDownloader = Task.CompletedTask;
			if (blog.DumpCrawlerData)
			{
				crawlerDownloader = crawlerDataDownloader.DownloadCrawlerDataAsync();
			}

			Tuple<ulong, bool> grabberResult = await grabber;
			bool apiLimitHit = grabberResult.Item2;

			UpdateProgressQueueInformation(Resources.ProgressUniqueDownloads);

			blog.DuplicatePhotos = DetermineDuplicates<PhotoPost>();
			blog.DuplicateVideos = DetermineDuplicates<VideoPost>();
			blog.DuplicateAudios = DetermineDuplicates<AudioPost>();
			blog.TotalCount = blog.TotalCount - blog.DuplicatePhotos - blog.DuplicateAudios - blog.DuplicateVideos;

			CleanCollectedBlogStatistics();

			await crawlerDownloader;
			bool finishedDownloading = await download;

			if (!ct.IsCancellationRequested)
			{
				blog.LastCompleteCrawl = DateTime.Now;
				if (finishedDownloading && !apiLimitHit)
				{
					blog.LastId = grabberResult.Item1;
				}
			}

			blog.Save();

			UpdateProgressQueueInformation("");
		}

		private string GetApiUrl(string url, int count, int start = 0)
		{
			if (url.Last<char>() != '/')
			{
				url += "/api/read/json?debug=1&";
			}
			else
			{
				url += "api/read/json?debug=1&";
			}

			Dictionary<string, string> parameters = new Dictionary<string, string>
			{
				{"num", count.ToString()}
			};
			if (start > 0)
			{
				parameters["start"] = start.ToString();
			}

			return url + UrlEncode(parameters);
		}

		private async Task<string> GetApiPageAsync(int pageId)
		{
			string url = GetApiUrl(blog.Url, blog.PageSize, pageId * blog.PageSize);

			if (shellService.Settings.LimitConnections)
			{
				crawlerService.Timeconstraint.Acquire();
				return await RequestDataAsync(url).TimeoutAfter(shellService.Settings.TimeOut);
			}

			return await RequestDataAsync(url).TimeoutAfter(shellService.Settings.TimeOut);
		}

		private async Task UpdateTotalPostCountAsync()
		{
			try
			{
				await UpdateTotalPostCount();
			}
			catch (WebException webException)
			{
				int webRespStatusCode = (int) ((HttpWebResponse) webException.Response).StatusCode;
				if (webRespStatusCode == 429)
				{
					Logger.Error("TumblrBlogCrawler:UpdateTotalPostCountAsync:WebException {0}", webException);
					shellService.ShowError(webException, Resources.LimitExceeded, blog.Name);
				}

				blog.Posts = 0;
			}
		}

		private async Task UpdateTotalPostCount()
		{
			string document = await GetApiPageAsync(0);
			TumblrApiJson response = ConvertJsonToClass<TumblrApiJson>(document);
			int totalPosts = response.posts_total;
			blog.Posts = totalPosts;
		}

		private async Task<ulong> GetHighestPostIdAsync()
		{
			try
			{
				return await GetHighestPostId();
			}
			catch (WebException webException)
			{
				int webRespStatusCode = (int) ((HttpWebResponse) webException.Response).StatusCode;
				if (webRespStatusCode == 429)
				{
					Logger.Error("TumblrBlogCrawler:GetHighestPostIdAsync:WebException {0}", webException);
					shellService.ShowError(webException, Resources.LimitExceeded, blog.Name);
				}

				return 0;
			}
		}

		private async Task<ulong> GetHighestPostId()
		{
			string document = await GetApiPageAsync(0);
			TumblrApiJson response = ConvertJsonToClass<TumblrApiJson>(document);

			ulong highestId;
			ulong.TryParse(response.posts?.FirstOrDefault()?.id, out highestId);
			return highestId;
		}

		private async Task<Tuple<ulong, bool>> GetUrlsAsync()
		{
			SemaphoreSlim semaphoreSlim = new SemaphoreSlim(shellService.Settings.ConcurrentScans);
			List<Task> trackedTasks = new List<Task>();
			bool apiLimitHit = false;
			bool completeGrab = true;

			await UpdateTotalPostCountAsync();
			int totalPosts = blog.Posts;

			ulong highestId = await GetHighestPostIdAsync();

			foreach (int pageNumber in GetPageNumbers())
			{
				await semaphoreSlim.WaitAsync();

				if (!completeGrab)
				{
					break;
				}

				if (ct.IsCancellationRequested)
				{
					break;
				}

				if (pt.IsPaused)
				{
					pt.WaitWhilePausedWithResponseAsyc().Wait();
				}

				trackedTasks.Add(new Func<Task>(async () =>
				{
					try
					{
						string document = await GetApiPageAsync(pageNumber);
						TumblrApiJson response = ConvertJsonToClass<TumblrApiJson>(document);

						completeGrab = CheckPostAge(response);

						if (!string.IsNullOrWhiteSpace(blog.Tags))
						{
							tags = blog.Tags.Split(',').Select(x => x.Trim()).ToList();
						}

						await AddUrlsToDownloadList(response);
					}
					catch (WebException webException) when (webException.Response != null)
					{
						int webRespStatusCode = (int) ((HttpWebResponse) webException.Response).StatusCode;
						if (webRespStatusCode == 429)
						{
							apiLimitHit = true;
							Logger.Error("TumblrBlogCrawler:GetUrls:WebException {0}", webException);
							shellService.ShowError(webException, Resources.LimitExceeded, blog.Name);
						}
					}
					catch (TimeoutException timeoutException)
					{
						Logger.Error("TumblrBlogCrawler:GetUrls:WebException {0}", timeoutException);
						shellService.ShowError(timeoutException, Resources.TimeoutReached, Resources.Crawling, blog.Name);
					}
					catch
					{
					}
					finally
					{
						semaphoreSlim.Release();
					}

					numberOfPagesCrawled += blog.PageSize;
					UpdateProgressQueueInformation(Resources.ProgressGetUrlLong, numberOfPagesCrawled, totalPosts);
				})());
			}

			await Task.WhenAll(trackedTasks);

			postQueue.CompleteAdding();
			jsonQueue.CompleteAdding();

			UpdateBlogStats();

			return new Tuple<ulong, bool>(highestId, apiLimitHit);
		}

		private bool PostWithinTimeSpan(Post post)
		{
			if (!(string.IsNullOrEmpty(blog.DownloadFrom) && string.IsNullOrEmpty(blog.DownloadTo)))
			{
				long downloadFromUnixTime = 0;
				long downloadToUnixTime = long.MaxValue;
				if (!string.IsNullOrEmpty(blog.DownloadFrom))
				{
					DateTime downloadFrom = DateTime.ParseExact(blog.DownloadFrom, "yyyyMMdd", CultureInfo.InvariantCulture,
						DateTimeStyles.None);
					downloadFromUnixTime = new DateTimeOffset(downloadFrom).ToUnixTimeSeconds();
				}

				if (!string.IsNullOrEmpty(blog.DownloadTo))
				{
					DateTime downloadTo = DateTime.ParseExact(blog.DownloadTo, "yyyyMMdd", CultureInfo.InvariantCulture,
						DateTimeStyles.None);
					downloadToUnixTime = new DateTimeOffset(downloadTo).ToUnixTimeSeconds();
				}

				long postTime;
				postTime = post.unix_timestamp;
				if ((downloadFromUnixTime >= postTime) || (postTime >= downloadToUnixTime))
				{
					return false;
				}
			}

			return true;
		}

		private bool CheckPostAge(TumblrApiJson response)
		{
			ulong highestPostId;
			ulong.TryParse(response.posts.FirstOrDefault()?.id,
				out highestPostId);

			if (highestPostId < GetLastPostId())
			{
				return false;
			}

			return true;
		}

		private void AddToJsonQueue(TumblrCrawlerData<Post> addToList)
		{
			if (blog.DumpCrawlerData)
			{
				jsonQueue.Add(addToList);
			}
		}

		private async Task AddUrlsToDownloadList(TumblrApiJson document)
		{
			try
			{
				AddPhotoUrlToDownloadList(document);
				AddVideoUrlToDownloadList(document);
				AddAudioUrlToDownloadList(document);
				AddTextUrlToDownloadList(document);
				AddQuoteUrlToDownloadList(document);
				AddLinkUrlToDownloadList(document);
				AddConversationUrlToDownloadList(document);
				AddAnswerUrlToDownloadList(document);
				AddPhotoMetaUrlToDownloadList(document);
				AddVideoMetaUrlToDownloadList(document);
				AddAudioMetaUrlToDownloadList(document);
				await AddExternalPhotoUrlToDownloadList(document);
			}
			catch (NullReferenceException)
			{
			}
		}

		private bool CheckIfDownloadRebloggedPosts(Post post)
		{
			if (!blog.DownloadRebloggedPosts)
			{
				if (!post.reblogged_from_url.Any())
				{
					return true;
				}

				return false;
			}

			return true;
		}

		private void AddPhotoUrlToDownloadList(TumblrApiJson document)
		{
			if (blog.DownloadPhoto)
			{
				foreach (Post post in document.posts)
				{
					if (!PostWithinTimeSpan(post))
					{
						continue;
					}

					if ((post.type == "photo") && (!tags.Any() ||
					                               post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase))))
					{
						if (CheckIfDownloadRebloggedPosts(post))
						{
							AddPhotoUrl(post);
							AddPhotoSetUrl(post);
							if (post.photo_caption != null)
							{
								//var postCopy = (Post)post.Clone();
								AddInlinePhotoUrl(post);
							}
						}
					}
				}

				// check for inline images
				foreach (Post post in document.posts.Where(p => p.type != "photo"))
				{
					if (!PostWithinTimeSpan(post))
					{
						continue;
					}

					if (!tags.Any() || post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase)))
					{
						if (CheckIfDownloadRebloggedPosts(post))
						{
							AddInlinePhotoUrl(post);
						}
					}
				}
			}
		}

		private void AddVideoUrlToDownloadList(TumblrApiJson document)
		{
			if (blog.DownloadVideo)
			{
				foreach (Post post in document.posts)
				{
					if (!PostWithinTimeSpan(post))
					{
						continue;
					}

					if ((post.type == "video") && (!tags.Any() ||
					                               post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase))))
					{
						if (CheckIfDownloadRebloggedPosts(post))
						{
							AddVideoUrl(post);
						}
					}
				}

				// check for inline videos
				foreach (Post post in document.posts.Where(p => p.type != "video"))
				{
					if (!PostWithinTimeSpan(post))
					{
						continue;
					}

					if (!tags.Any() || post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase)))
					{
						if (CheckIfDownloadRebloggedPosts(post))
						{
							if (post.video_caption != null)
							{
								//var postCopy = (Post)post.Clone();
								AddInlineVideoUrl(post);
							}
						}
					}
				}
			}
		}

		private void AddAudioUrlToDownloadList(TumblrApiJson document)
		{
			if (blog.DownloadAudio)
			{
				foreach (Post post in document.posts)
				{
					if (!PostWithinTimeSpan(post))
					{
						continue;
					}

					if ((post.type == "audio") && (!tags.Any() ||
					                               post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase))))
					{
						if (CheckIfDownloadRebloggedPosts(post))
						{
							AddAudioUrl(post);
						}
					}
				}
			}
		}

		private void AddTextUrlToDownloadList(TumblrApiJson document)
		{
			if (blog.DownloadText)
			{
				foreach (Post post in document.posts)
				{
					if (!PostWithinTimeSpan(post))
					{
						continue;
					}

					if ((post.type == "regular") && (!tags.Any() ||
					                                 post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase))))
					{
						if (CheckIfDownloadRebloggedPosts(post))
						{
							string textBody = tumblrJsonParser.ParseText(post);
							AddToDownloadList(new TextPost(textBody, post.id));
							AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.id, ".json"), post));
						}
					}
				}
			}
		}

		private void AddQuoteUrlToDownloadList(TumblrApiJson document)
		{
			if (blog.DownloadQuote)
			{
				foreach (Post post in document.posts)
				{
					if (!PostWithinTimeSpan(post))
					{
						continue;
					}

					if ((post.type == "quote") && (!tags.Any() ||
					                               post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase))))
					{
						if (CheckIfDownloadRebloggedPosts(post))
						{
							string textBody = tumblrJsonParser.ParseQuote(post);
							AddToDownloadList(new QuotePost(textBody, post.id));
							AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.id, ".json"), post));
						}
					}
				}
			}
		}

		private void AddLinkUrlToDownloadList(TumblrApiJson document)
		{
			if (blog.DownloadLink)
			{
				foreach (Post post in document.posts)
				{
					if (!PostWithinTimeSpan(post))
					{
						continue;
					}

					if ((post.type == "link") && (!tags.Any() ||
					                              post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase))))
					{
						if (CheckIfDownloadRebloggedPosts(post))
						{
							string textBody = tumblrJsonParser.ParseLink(post);
							AddToDownloadList(new LinkPost(textBody, post.id));
							AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.id, ".json"), post));
						}
					}
				}
			}
		}

		private void AddConversationUrlToDownloadList(TumblrApiJson document)
		{
			if (blog.DownloadConversation)
			{
				foreach (Post post in document.posts)
				{
					if (!PostWithinTimeSpan(post))
					{
						continue;
					}

					if ((post.type == "conversation") && (!tags.Any() ||
					                                      post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase))))
					{
						if (CheckIfDownloadRebloggedPosts(post))
						{
							string textBody = tumblrJsonParser.ParseConversation(post);
							AddToDownloadList(new ConversationPost(textBody, post.id));
							AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.id, ".json"), post));
						}
					}
				}
			}
		}

		private void AddAnswerUrlToDownloadList(TumblrApiJson document)
		{
			if (blog.DownloadAnswer)
			{
				foreach (Post post in document.posts)
				{
					if (!PostWithinTimeSpan(post))
					{
						continue;
					}

					if ((post.type == "answer") && (!tags.Any() ||
					                                post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase))))
					{
						if (CheckIfDownloadRebloggedPosts(post))
						{
							string textBody = tumblrJsonParser.ParseAnswer(post);
							AddToDownloadList(new AnswerPost(textBody, post.id));
							AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.id, ".json"), post));
						}
					}
				}
			}
		}

		private void AddPhotoMetaUrlToDownloadList(TumblrApiJson document)
		{
			if (blog.CreatePhotoMeta)
			{
				foreach (Post post in document.posts)
				{
					if (!PostWithinTimeSpan(post))
					{
						continue;
					}

					if ((post.type == "photo") && (!tags.Any() ||
					                               post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase))))
					{
						if (CheckIfDownloadRebloggedPosts(post))
						{
							string textBody = tumblrJsonParser.ParsePhotoMeta(post);
							AddToDownloadList(new PhotoMetaPost(textBody, post.id));
							AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.id, ".json"), post));
						}
					}
				}
			}
		}

		private void AddVideoMetaUrlToDownloadList(TumblrApiJson document)
		{
			if (blog.CreateVideoMeta)
			{
				foreach (Post post in document.posts)
				{
					if (!PostWithinTimeSpan(post))
					{
						continue;
					}

					if ((post.type == "video") && (!tags.Any() ||
					                               post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase))))
					{
						if (CheckIfDownloadRebloggedPosts(post))
						{
							string textBody = tumblrJsonParser.ParseVideoMeta(post);
							AddToDownloadList(new VideoMetaPost(textBody, post.id));
							AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.id, ".json"), post));
						}
					}
				}
			}
		}

		private void AddAudioMetaUrlToDownloadList(TumblrApiJson document)
		{
			if (blog.CreateAudioMeta)
			{
				foreach (Post post in document.posts)
				{
					if (!PostWithinTimeSpan(post))
					{
						continue;
					}

					if ((post.type == "audio") && (!tags.Any() ||
					                               post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase))))
					{
						if (CheckIfDownloadRebloggedPosts(post))
						{
							string textBody = tumblrJsonParser.ParseAudioMeta(post);
							AddToDownloadList(new AudioMetaPost(textBody, post.id));
							AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(post.id, ".json"), post));
						}
					}
				}
			}
		}

		private string ParseImageUrl(Post post)
		{
			string imageUrl = (string) post.GetType().GetProperty("photo_url_" + ImageSize())?.GetValue(post, null) ?? post.photo_url_1280;
			return imageUrl;
		}

		private string ParseImageUrl(Photo post)
		{
			string imageUrl = (string) post.GetType().GetProperty("photo_url_" + ImageSize())?.GetValue(post, null) ?? post.photo_url_1280;
			return imageUrl;
		}

		private string InlineSearch(Post post)
		{
			return string.Join(" ", post.photo_caption, post.video_caption, post.audio_caption,
				post.conversation_text, post.regular_body, post.answer, post.photos.Select(photo => photo.caption),
				post.conversation.Select(conversation => conversation.phrase));
		}

		private void AddInlinePhotoUrl(Post post)
		{
			Regex regex = new Regex("\"(http[A-Za-z0-9_/:.]*media.tumblr.com[A-Za-z0-9_/:.]*(jpg|png|gif))\"");
			foreach (Match match in regex.Matches(InlineSearch(post)))
			{
				string imageUrl = match.Groups[1].Value;
				if (imageUrl.Contains("avatar") || imageUrl.Contains("previews"))
				{
					continue;
				}

				if (blog.SkipGif && imageUrl.EndsWith(".gif"))
				{
					continue;
				}

				imageUrl = ResizeTumblrImageUrl(imageUrl);
				AddToDownloadList(new PhotoPost(imageUrl, post.id, post.unix_timestamp.ToString()));
				//AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
			}
		}

		private void AddInlineVideoUrl(Post post)
		{
			Regex regex = new Regex("\"(http[A-Za-z0-9_/:.]*.com/video_file/[A-Za-z0-9_/:.]*)\"");
			foreach (Match match in regex.Matches(InlineSearch(post)))
			{
				string videoUrl = match.Groups[1].Value;
				if (shellService.Settings.VideoSize == 1080)
				{
					AddToDownloadList(new VideoPost(videoUrl.Replace("/480", "") + ".mp4", post.id, post.unix_timestamp.ToString()));
					//AddToJsonQueue(new TumblrCrawlerXmlData(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
				}
				else if (shellService.Settings.VideoSize == 480)
				{
					AddToDownloadList(new VideoPost(
						"https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + "_480.mp4",
						post.id, post.unix_timestamp.ToString()));
					//AddToJsonQueue(new TumblrCrawlerXmlData(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
				}
			}
		}

		private void AddPhotoUrl(Post post)
		{
			string imageUrl = ParseImageUrl(post);
			if (blog.SkipGif && imageUrl.EndsWith(".gif"))
			{
				return;
			}

			AddToDownloadList(new PhotoPost(imageUrl, post.id, post.unix_timestamp.ToString()));
			AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
		}

		private void AddPhotoSetUrl(Post post)
		{
			if (!post.photos.Any())
			{
				return;
			}

			foreach (string imageUrl in post.photos.Select(ParseImageUrl).Where(imageUrl => !blog.SkipGif || !imageUrl.EndsWith(".gif")))
			{
				AddToDownloadList(new PhotoPost(imageUrl, post.id, post.unix_timestamp.ToString()));
				AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
			}
		}

		private void AddVideoUrl(Post post)
		{
			string videoUrl = Regex.Match(post.video_player, "<source src=\"([\\S]*)\"").Groups[1].ToString();

			if (shellService.Settings.VideoSize == 1080)
			{
				AddToDownloadList(new VideoPost(
					"https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + ".mp4",
					post.id, post.unix_timestamp.ToString()));
				AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
			}
			else if (shellService.Settings.VideoSize == 480)
			{
				AddToDownloadList(new VideoPost(
					"https://vt.tumblr.com/" + videoUrl.Replace("/480", "").Split('/').Last() + "_480.mp4",
					post.id, post.unix_timestamp.ToString()));
				AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
			}
		}

		private void AddAudioUrl(Post post)
		{
			string audioUrl = Regex.Match(post.audio_embed, "audio_file=([\\S]*)\"").Groups[1].ToString();
			audioUrl = System.Web.HttpUtility.UrlDecode(audioUrl);
			if (!audioUrl.EndsWith(".mp3"))
			{
				audioUrl = audioUrl + ".mp3";
			}

			AddToDownloadList(new AudioPost(WebUtility.UrlDecode(audioUrl), post.id, post.unix_timestamp.ToString()));
			AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(audioUrl.Split('/').Last(), ".json"), post));
		}

		private async Task AddExternalPhotoUrlToDownloadList(TumblrApiJson document)
		{
			if (blog.DownloadImgur) await AddImgurUrl(document);

			if (blog.DownloadGfycat) await AddGfycatUrl(document);

			if (blog.DownloadWebmshare) AddWebmshareUrl(document);

			if (blog.DownloadMixtape) AddMixtapeUrl(document);

			if (blog.DownloadMega) await AddMegaUrl(document);

			if (blog.DownloadGoogleDrive) AddGoogleDriveUrl(document);

			if (blog.DownloadUguu) AddUguuUrl(document);

			if (blog.DownloadSafeMoe) AddSafeMoeUrl(document);

			if (blog.DownloadLoliSafe) AddLoliSafeUrl(document);

			if (blog.DownloadCatBox) AddCatBoxUrl(document);
		}

		private async Task AddImgurUrl(TumblrApiJson document)
		{
			foreach (Post post in document.posts)
			{
				if (!PostWithinTimeSpan(post))
				{
					continue;
				}

				if (!tags.Any() || post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase)))
				{
					if (CheckIfDownloadRebloggedPosts(post))
					{
						// single linked images
						Regex regex = imgurParser.GetImgurImageRegex();
						foreach (Match match in regex.Matches(InlineSearch(post)))
						{
							string imageUrl = match.Groups[1].Value;
							string imgurId = match.Groups[2].Value;
							if (blog.SkipGif && (imageUrl.EndsWith(".gif") || imageUrl.EndsWith(".gifv")))
							{
								continue;
							}

							AddToDownloadList(new ExternalPhotoPost(imageUrl, imgurId,
								post.unix_timestamp.ToString()));
							AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
						}

						// album urls
						regex = imgurParser.GetImgurAlbumRegex();
						foreach (Match match in regex.Matches(InlineSearch(post)))
						{
							string albumUrl = match.Groups[1].Value;
							string imgurId = match.Groups[2].Value;
							string album = await imgurParser.RequestImgurAlbumSite(albumUrl);

							Regex hashRegex = imgurParser.GetImgurAlbumHashRegex();
							MatchCollection hashMatches = hashRegex.Matches(album);
							List<string> hashes = hashMatches.Cast<Match>().Select(hashMatch => hashMatch.Groups[1].Value).ToList();

							Regex extRegex = imgurParser.GetImgurAlbumExtRegex();
							MatchCollection extMatches = extRegex.Matches(album);
							List<string> exts = extMatches.Cast<Match>().Select(extMatch => extMatch.Groups[1].Value).ToList();

							IEnumerable<string> imageUrls = hashes.Zip(exts, (hash, ext) => "https://i.imgur.com/" + hash + ext);

							foreach (string imageUrl in imageUrls)
							{
								if (blog.SkipGif && (imageUrl.EndsWith(".gif") || imageUrl.EndsWith(".gifv")))
								{
									continue;
								}

								AddToDownloadList(new ExternalPhotoPost(imageUrl, imgurId,
									post.unix_timestamp.ToString()));
								AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
							}
						}
					}
				}
			}
		}

		private async Task AddGfycatUrl(TumblrApiJson document)
		{
			foreach (Post post in document.posts)
			{
				if (!PostWithinTimeSpan(post))
				{
					continue;
				}

				if (!tags.Any() || post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase)))
				{
					if (CheckIfDownloadRebloggedPosts(post))
					{
						Regex regex = gfycatParser.GetGfycatUrlRegex();
						foreach (Match match in regex.Matches(InlineSearch(post)))
						{
							string gfyId = match.Groups[2].Value;
							string videoUrl = gfycatParser.ParseGfycatCajaxResponse(await gfycatParser.RequestGfycatCajax(gfyId),
								blog.GfycatType);
							if (blog.SkipGif && (videoUrl.EndsWith(".gif") || videoUrl.EndsWith(".gifv")))
							{
								continue;
							}

							AddToDownloadList(new ExternalVideoPost(videoUrl, gfyId,
								post.unix_timestamp.ToString()));
							AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
						}
					}
				}
			}
		}

		private void AddWebmshareUrl(TumblrApiJson document)
		{
			foreach (Post post in document.posts)
			{
				if (!PostWithinTimeSpan(post))
				{
					continue;
				}

				if (!tags.Any() || post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase)))
				{
					if (CheckIfDownloadRebloggedPosts(post))
					{
						Regex regex = webmshareParser.GetWebmshareUrlRegex();
						foreach (Match match in regex.Matches(InlineSearch(post)))
						{
							string webmshareId = match.Groups[2].Value;
							string url = match.Groups[0].Value.Split('\"').First();
							string imageUrl = webmshareParser.CreateWebmshareUrl(webmshareId, url, blog.WebmshareType);
							if (blog.SkipGif && (imageUrl.EndsWith(".gif") || imageUrl.EndsWith(".gifv")))
							{
								continue;
							}

							AddToDownloadList(new ExternalVideoPost(imageUrl, webmshareId,
								post.unix_timestamp.ToString()));
							AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
						}
					}
				}
			}
		}

		private void AddMixtapeUrl(TumblrApiJson document)
		{
			foreach (Post post in document.posts)
			{
				if (!PostWithinTimeSpan(post))
				{
					continue;
				}

				if (!tags.Any() || post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase)))
				{
					if (CheckIfDownloadRebloggedPosts(post))
					{
						Regex regex = mixtapeParser.GetMixtapeUrlRegex();
						string[] parts = InlineSearch(post).Split(new string[] { "href=" }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string part in parts)
						{
							
							foreach (Match match in regex.Matches(part))
							{
								string temp = match.Groups[0].ToString();
								string id = match.Groups[2].Value;
								string url = temp.Split('\"').First();

								string imageUrl = mixtapeParser.CreateMixtapeUrl(id, url, blog.MixtapeType);
								if (blog.SkipGif && imageUrl.EndsWith(".gif"))
								{
									continue;
								}

								AddToDownloadList(new ExternalVideoPost(imageUrl, id,
									post.unix_timestamp.ToString()));
								AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));

							}

						}
					}
				}
			}
		}

		private async Task AddMegaUrl(TumblrApiJson document)
		{
			foreach (Post post in document.posts)
			{
				if (!PostWithinTimeSpan(post))
				{
					continue;
				}

				if (!tags.Any() || post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase)))
				{
					if (CheckIfDownloadRebloggedPosts(post))
					{
						Regex regex = megaParser.GetMegaUrlRegex();
						string[] parts = InlineSearch(post).Split(new string[] { "href=" }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string part in parts)
						{
							foreach (Match match in regex.Matches(part))
							{
								string temp = match.Groups[0].ToString();
								string id = match.Groups[2].Value;
								string url = temp.Split('\"').First();

                                var list = await megaParser.GetUrls(url);

                                var urls = list.Take(list.Count() / 2);
                                var names = list.Skip(list.Count() / 2);

                                var urlsNames = urls.Zip(names, (urlname, name) => new { Url = urlname, Name = name });
                                foreach (var urlName in urlsNames)
                                {
                                    if (blog.SkipGif && urlName.Url.EndsWith(".gif"))
                                    {
                                        continue;
                                    }

                                    AddToDownloadList(new ExternalVideoPost(urlName.Url, urlName.Name, post.unix_timestamp.ToString()));
                                }
							}
						}
					}
				}
			}
		}

		private void AddGoogleDriveUrl(TumblrApiJson document)
		{
		
			foreach (Post post in document.posts)
			{
				if (!PostWithinTimeSpan(post))
				{
					continue;
				}

				if (!tags.Any() || post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase)))
				{
					if (CheckIfDownloadRebloggedPosts(post))
					{
						Regex regex = googledriveParser.GetGoogleDriveUrlRegex();
						string[] parts = InlineSearch(post).Split(new string[] { "href=" }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string part in parts)
						{
							foreach (Match match in regex.Matches(part))
							{
								string temp = match.Groups[0].ToString();
								string id = match.Groups[2].Value;
								string url =temp.Split('\"').First();

								string imageUrl = googledriveParser.CreateGoogleDriveUrl(id, url, blog.GoogleDriveType);
								if (blog.SkipGif && imageUrl.EndsWith(".gif"))
								{
									continue;
								}

								AddToDownloadList(new ExternalVideoPost(imageUrl, string.Empty, post.unix_timestamp.ToString()));
								AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(url, ".json"), post));

							}
						}
					}
				}
			}
		}

		private void AddUguuUrl(TumblrApiJson document)
		{
			foreach (Post post in document.posts)
			{
				if (!PostWithinTimeSpan(post))
				{
					continue;
				}

				if (!tags.Any() || post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase)))
				{
					if (CheckIfDownloadRebloggedPosts(post))
					{
						Regex regex = uguuParser.GetUguuUrlRegex();
						string[] parts = InlineSearch(post).Split(new string[] { "href=" }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string part in parts)
						{
							
							foreach (Match match in regex.Matches(part))
							{
								string temp = match.Groups[0].ToString();
								string id = match.Groups[2].Value;
								string url =temp.Split('\"').First();


								string imageUrl = uguuParser.CreateUguuUrl(id, url, blog.UguuType);
								if (blog.SkipGif && imageUrl.EndsWith(".gif"))
								{
									continue;
								}

								AddToDownloadList(new ExternalVideoPost(imageUrl, id,
									post.unix_timestamp.ToString()));
								AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));

							}

						}
					}
				}
			}
		}

		private void AddSafeMoeUrl(TumblrApiJson document)
		{
			foreach (Post post in document.posts)
			{
				if (!PostWithinTimeSpan(post))
				{
					continue;
				}

				if (!tags.Any() || post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase)))
				{
					if (CheckIfDownloadRebloggedPosts(post))
					{
						Regex regex = safemoeParser.GetSafeMoeUrlRegex();
						string[] parts = InlineSearch(post).Split(new string[] { "href=" }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string part in parts)
						{
							
							foreach (Match match in regex.Matches(part))
							{
								string temp = match.Groups[0].ToString();
								string id = match.Groups[2].Value;
								string url =temp.Split('\"').First();


								string imageUrl = safemoeParser.CreateSafeMoeUrl(id, url, blog.SafeMoeType);
								if (blog.SkipGif && imageUrl.EndsWith(".gif"))
								{
									continue;
								}

								AddToDownloadList(new ExternalVideoPost(imageUrl, id,
									post.unix_timestamp.ToString()));
								AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));

							}

						}
					}
				}
			}
		}

		private void AddLoliSafeUrl(TumblrApiJson document)
		{
			foreach (Post post in document.posts)
			{
				if (!PostWithinTimeSpan(post))
				{
					continue;
				}

				if (!tags.Any() || post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase)))
				{
					if (CheckIfDownloadRebloggedPosts(post))
					{
						Regex regex = lolisafeParser.GetLoliSafeUrlRegex();
						string[] parts = InlineSearch(post).Split(new string[] { "href=" }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string part in parts)
						{
							
							foreach (Match match in regex.Matches(part))
							{
								string temp = match.Groups[0].ToString();
								string id = match.Groups[2].Value;
								string url =temp.Split('\"').First();

								string imageUrl = lolisafeParser.CreateLoliSafeUrl(id, url, blog.LoliSafeType);
								if (blog.SkipGif && imageUrl.EndsWith(".gif"))
								{
									continue;
								}

								AddToDownloadList(new ExternalVideoPost(imageUrl, id,
									post.unix_timestamp.ToString()));
								AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));

							}

						}
					}
				}
			}
		}

		private void AddCatBoxUrl(TumblrApiJson document)
		{
			foreach (Post post in document.posts)
			{
				if (!PostWithinTimeSpan(post))
				{
					continue;
				}

				if (!tags.Any() || post.tags.Any(x => tags.Contains(x, StringComparer.OrdinalIgnoreCase)))
				{
					if (CheckIfDownloadRebloggedPosts(post))
					{
						Regex regex = catboxParser.GetCatBoxUrlRegex();
						string[] parts = InlineSearch(post).Split(new string[] { "href=" }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string part in parts)
						{
							
							foreach (Match match in regex.Matches(part))
							{
								string temp = match.Groups[0].ToString();
								string id = match.Groups[2].Value;
								string url =temp.Split('\"').First();

								string imageUrl = catboxParser.CreateCatBoxUrl(id, url, blog.CatBoxType);
								if (blog.SkipGif && imageUrl.EndsWith(".gif"))
								{
									continue;
								}

								AddToDownloadList(new ExternalVideoPost(imageUrl, id,
									post.unix_timestamp.ToString()));
								AddToJsonQueue(new TumblrCrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));

							}

						}
					}
				}
			}
		}
	}
}