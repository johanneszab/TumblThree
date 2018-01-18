using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CG.Web.MegaApiClient;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Extensions;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using File = Google.Apis.Drive.v3.Data.File;

namespace TumblThree.Applications.Downloader
{
	public abstract class AbstractDownloader : IDownloader
	{
		protected readonly IBlog blog;
		protected readonly IFiles files;
		protected readonly ICrawlerService crawlerService;
		private readonly IManagerService managerService;
		protected readonly IProgress<DownloadProgress> progress;
		protected readonly object lockObjectDownload = new object();
		protected readonly IPostQueue<TumblrPost> postQueue;
		protected readonly IShellService shellService;
		protected readonly CancellationToken ct;
		protected readonly PauseToken pt;
		protected readonly FileDownloader fileDownloader;

		private string[] suffixes = {".jpg", ".jpeg", ".png"};

		protected AbstractDownloader(IShellService shellService, IManagerService managerService, CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress, IPostQueue<TumblrPost> postQueue, FileDownloader fileDownloader, ICrawlerService crawlerService = null, IBlog blog = null, IFiles files = null)
		{
			this.shellService = shellService;
			this.crawlerService = crawlerService;
			this.managerService = managerService;
			this.blog = blog;
			this.files = files;
			this.ct = ct;
			this.pt = pt;
			this.progress = progress;
			this.postQueue = postQueue;
			this.fileDownloader = fileDownloader;
		}

		public void UpdateProgressQueueInformation(string format, params object[] args)
		{
			DownloadProgress newProgress = new DataModels.DownloadProgress
			{
				Progress = string.Format(CultureInfo.CurrentCulture, format, args)
			};
			progress.Report(newProgress);
		}

		protected virtual string GetCoreImageUrl(string url)
		{
			return url;
		}

		protected virtual async Task<bool> DownloadBinaryFile(string fileLocation, string url, INode node = null)
		{
			try
			{
				return await fileDownloader.DownloadFileWithResumeAsync(url, fileLocation, node).TimeoutAfter(shellService.Settings.TimeOut);
			}
			catch (IOException ex) when (((ex.HResult & 0xFFFF) == 0x27) || ((ex.HResult & 0xFFFF) == 0x70))
			{
				// Disk Full, HRESULT: ‭-2147024784‬ == 0xFFFFFFFF80070070
				Logger.Error("AbstractDownloader:DownloadBinaryFile: {0}", ex);
				shellService.ShowError(ex, Resources.DiskFull);
				crawlerService.StopCommand.Execute(null);
				return false;
			}
			catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x20)
			{
				// The process cannot access the file because it is being used by another process.", HRESULT: -2147024864 == 0xFFFFFFFF80070020
				return true;
			}
			catch (WebException webException) when (webException.Response != null)
			{
				int webRespStatusCode = (int) ((HttpWebResponse) webException.Response).StatusCode;
				if ((webRespStatusCode >= 400) && (webRespStatusCode < 600)) // removes inaccessible files: http status codes 400 to 599
				{
					try
					{
						System.IO.File.Delete(fileLocation);
					} // could be open again in a different thread
					catch
					{
					}
				}

				return false;
			}
			catch (TimeoutException timeoutException)
			{
				Logger.Error("AbstractDownloader:DownloadBinaryFile {0}", timeoutException);
				shellService.ShowError(timeoutException, Resources.TimeoutReached, Resources.Downloading, blog.Name);
				return false;
			}
			catch
			{
				return false;
			}
		}

		protected virtual async Task<bool> DownloadBinaryFile(string fileLocation, string fileLocationUrlList, string url, INode node = null)
		{
			if (!blog.DownloadUrlList)
			{
				return await DownloadBinaryFile(fileLocation, url, node);
			}

			return AppendToTextFile(fileLocationUrlList, url);
		}

		protected virtual bool AppendToTextFile(string fileLocation, string text)
		{
			try
			{
				lock (lockObjectDownload)
				{
					using (StreamWriter sw = new StreamWriter(fileLocation, true))
					{
						sw.WriteLine(text);
					}
				}

				return true;
			}
			catch (IOException ex) when (((ex.HResult & 0xFFFF) == 0x27) || ((ex.HResult & 0xFFFF) == 0x70))
			{
				Logger.Error("Downloader:AppendToTextFile: {0}", ex);
				shellService.ShowError(ex, Resources.DiskFull);
				crawlerService.StopCommand.Execute(null);
				return false;
			}
			catch
			{
				return false;
			}
		}

		public virtual async Task<bool> DownloadBlogAsync()
		{
			SemaphoreSlim concurrentConnectionsSemaphore = new SemaphoreSlim(shellService.Settings.ConcurrentConnections / crawlerService.ActiveItems.Count);
			SemaphoreSlim concurrentVideoConnectionsSemaphore = new SemaphoreSlim(shellService.Settings.ConcurrentVideoConnections / crawlerService.ActiveItems.Count);
			List<Task> trackedTasks = new List<Task>();
			bool completeDownload = true;

			blog.CreateDataFolder();

			foreach (TumblrPost downloadItem in postQueue.GetConsumingEnumerable())
			{
				if (downloadItem.GetType() == typeof(VideoPost))
				{
					await concurrentVideoConnectionsSemaphore.WaitAsync();
				}

				await concurrentConnectionsSemaphore.WaitAsync();

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
						await DownloadPostAsync(downloadItem);
					}
					catch
					{
					}
					finally
					{
						concurrentConnectionsSemaphore.Release();
						if (downloadItem.GetType() == typeof(VideoPost))
						{
							concurrentVideoConnectionsSemaphore.Release();
						}
					}
				})());
			}

			try
			{
				await Task.WhenAll(trackedTasks);
			}
			catch
			{
				completeDownload = false;
			}

			blog.LastDownloadedPhoto = null;
			blog.LastDownloadedVideo = null;

			files.Save();

			return completeDownload;
		}

		private async Task DownloadPostAsync(TumblrPost downloadItem)
		{
			// TODO: Refactor, should be polymorphism
			if (downloadItem.PostType == PostType.Binary)
			{
				await DownloadBinaryPost(downloadItem);
			}
			else
			{
				DownloadTextPost(downloadItem);
			}
		}

		protected virtual async Task<bool> DownloadBinaryPost(TumblrPost downloadItem)
		{
			string url = Url(downloadItem);

			if (!CheckIfFileExistsInDB(url))
			{
				string blogDownloadLocation = blog.DownloadLocation();
				string fileLocationUrlList = FileLocationLocalized(blogDownloadLocation, downloadItem.TextFileLocation);
				DateTime postDate = PostDate(downloadItem);


				string fileName = FileName(downloadItem);
				string fileLocation = FileLocation(blogDownloadLocation, fileName);

				if (url.Contains("https://mega.nz/#"))
				{
					Uri link = new Uri(url);

					Crawler.MegaLinkType linkType = Crawler.MegaLinkType.Single;
					//Determines if the MEGA link is a folder or single file based on if the url is mega.nz/#! or mega.nz/#F
					Regex regType = new Regex("(http[A-Za-z0-9_/:.]*mega.nz/#(.*)([A-Za-z0-9_]*))");
					foreach (Match rmatch in regType.Matches(url))
					{
						string subStr = rmatch.Groups[2].Value[0].ToString();

						if (subStr == "!") linkType = Crawler.MegaLinkType.Single;
						if (subStr == "F") linkType = Crawler.MegaLinkType.Folder;
					}

					MegaApiClient client = new MegaApiClient();
					client.LoginAnonymous();

					switch (linkType)
					{
						case Crawler.MegaLinkType.Single:
							INodeInfo nodeInfo = client.GetNodeFromLink(link);
							fileName = nodeInfo.Name;
							fileLocation = FileLocation(blogDownloadLocation, fileName);

							UpdateProgressQueueInformation(Resources.ProgressDownloadImage, fileName);
							if (await DownloadBinaryFile(fileLocation, fileLocationUrlList, url))
							{
								updateBlog(fileLocation, postDate, downloadItem, fileName);
								return true;
							}

							client.Logout();
							return false;


						case Crawler.MegaLinkType.Folder:
							//If the link is a folder, download all files from it.


							IEnumerable<INode> nodes = client.GetNodesFromLink(link);

							List<INode> allFiles = nodes.Where(n => n.Type == NodeType.File).ToList();

							foreach (INode node in allFiles)
							{
								fileName = node.Name;

								fileLocation = FileLocation(blogDownloadLocation, fileName);
								UpdateProgressQueueInformation(Resources.ProgressDownloadImage, fileName);
								if (await DownloadBinaryFile(fileLocation, fileLocationUrlList, url, node))
								{
									updateBlog(fileLocation, postDate, downloadItem, fileName);
								}
							}

							client.Logout();
							return false;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				if (url.Contains("https://drive.google.com/"))
				{
					Console.Write("url:" + url + "\n");
					UserCredential credentials = Authenticate();
					DriveService service = OpenService(credentials);
					RequestInfo(service, url, blogDownloadLocation + "\\");
				}

				UpdateProgressQueueInformation(Resources.ProgressDownloadImage, fileName);
				if (await DownloadBinaryFile(fileLocation, fileLocationUrlList, url))
				{
					updateBlog(fileLocation, postDate, downloadItem, fileName);

					return true;
				}

				return false;
			}
			else
			{
				string fileName = FileName(downloadItem);
				UpdateProgressQueueInformation(Resources.ProgressSkipFile, fileName);
			}

			return true;
		}

		public void updateBlog(string fileLocation, DateTime postDate, TumblrPost downloadItem, string fileName)
		{
			SetFileDate(fileLocation, postDate);
			UpdateBlogDB(downloadItem.DbType, fileName);
			//TODO: Refactor
			if (shellService.Settings.EnablePreview)
			{
				if (suffixes.Any(suffix => fileName.EndsWith(suffix)))
				{
					blog.LastDownloadedPhoto = Path.GetFullPath(fileLocation);
				}
				else
				{
					blog.LastDownloadedVideo = Path.GetFullPath(fileLocation);
				}
			}
		}


		private bool CheckIfFileExistsInDB(string url)
		{
			if (shellService.Settings.LoadAllDatabases)
			{
				if (managerService.CheckIfFileExistsInDB(url))
				{
					return true;
				}
			}
			else
			{
				if (files.CheckIfFileExistsInDB(url) || blog.CheckIfBlogShouldCheckDirectory(GetCoreImageUrl(url)))
				{
					return true;
				}
			}

			return false;
		}

		private void DownloadTextPost(TumblrPost downloadItem)
		{
			string postId = PostId(downloadItem);
			if (!CheckIfFileExistsInDB(postId))
			{
				string blogDownloadLocation = blog.DownloadLocation();
				string url = Url(downloadItem);
				string fileLocation = FileLocationLocalized(blogDownloadLocation, downloadItem.TextFileLocation);
				UpdateProgressQueueInformation(Resources.ProgressDownloadImage, postId);
				if (AppendToTextFile(fileLocation, url))
				{
					UpdateBlogDB(downloadItem.DbType, postId);
				}
			}
			else
			{
				UpdateProgressQueueInformation(Resources.ProgressSkipFile, postId);
			}
		}

		private void UpdateBlogDB(string postType, string fileName)
		{
			blog.UpdatePostCount(postType);
			blog.UpdateProgress();
			files.AddFileToDb(fileName);
		}

		protected void SetFileDate(string fileLocation, DateTime postDate)
		{
			if (!blog.DownloadUrlList)
			{
				System.IO.File.SetLastWriteTime(fileLocation, postDate);
			}
		}

		protected static string Url(TumblrPost downloadItem)
		{
			return downloadItem.Url;
		}

		private static string FileName(TumblrPost downloadItem)
		{
			return downloadItem.Url.Split('/').Last();
		}

		protected static string FileLocation(string blogDownloadLocation, string fileName)
		{
			return Path.Combine(blogDownloadLocation, fileName);
		}

		protected static string FileLocationLocalized(string blogDownloadLocation, string fileName)
		{
			return Path.Combine(blogDownloadLocation, string.Format(CultureInfo.CurrentCulture, fileName));
		}

		private static string PostId(TumblrPost downloadItem)
		{
			return downloadItem.Id;
		}

		protected static DateTime PostDate(TumblrPost downloadItem)
		{
			if (!string.IsNullOrEmpty(downloadItem.Date))
			{
				DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
				DateTime postDate = epoch.AddSeconds(Convert.ToDouble(downloadItem.Date)).ToLocalTime();
				return postDate;
			}

			return DateTime.Now;
		}

		#region GoogleAPIUtils

		public UserCredential Authenticate()
		{
			string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), System.Waf.Applications.ApplicationInfo.Company, System.Waf.Applications.ApplicationInfo.ProductName, "Settings");
			string portablePath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
			string currentFolder = settingsPath ?? portablePath;
			string credentialsFolder = Path.Combine(currentFolder, "credential");

			return Authenticate(credentialsFolder);
		}

		public UserCredential Authenticate(string credentialsFolder)
		{
			UserCredential credentials;

			using (FileStream stream = new FileStream(Path.Combine(credentialsFolder, "google_secret.json"), FileMode.Open, FileAccess.Read))
			{
				// Delete credentials cache at folder debug/bin/credentials after changes here
				credentials = GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					new[]
					{
						DriveService.Scope.Drive,
						DriveService.Scope.DriveAppdata,
						DriveService.Scope.DriveFile,
						DriveService.Scope.DriveMetadata,
						DriveService.Scope.DriveScripts,
						//Google.Apis.Drive.v3.DriveService.Scope.DriveReadonly,
						DriveService.Scope.DrivePhotosReadonly
					},
					"user",
					CancellationToken.None,
					new FileDataStore(credentialsFolder, true)).Result;
			}

			return credentials;
		}

		public DriveService OpenService(UserCredential credentials)
		{
			return new DriveService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credentials
			});
		}

		public string getIdFromUrl(string url)
		{
			string id = "";
			string[] parts = url.Split('/');

			if (url.IndexOf("?id=") >= 0)
			{
				id = (parts[3].Split('=')[1].Replace("&usp", ""));
				return id;
			}

			string[] tempid = parts[5].Split('/');

			List<string> sortList = tempid.OrderBy(a => a).ToList();
			id = sortList[0];
			return id;
		}

		public File RequestInfo(DriveService service, string url, string path)
		{
			try
			{
				string fileId = getIdFromUrl(url);
				FilesResource.GetRequest request = service.Files.Get(fileId);
				File file = request.Execute();
				downloadFile(service, file, path + "\\");
			}
			catch (Exception e)
			{
				Console.WriteLine("An error occurred: " + e.Message);
			}

			return null;
		}

		public void downloadFile(DriveService service, File FileResource, string path)
		{
			if (FileResource.MimeType != "application/vnd.google-apps.folder")
			{
				MemoryStream stream = new MemoryStream();

				service.Files.Get(FileResource.Id).Download(stream);

				FileStream file = new FileStream(path + @"/" + FileResource.Name, FileMode.Create, FileAccess.Write);
				stream.WriteTo(file);
				file.Close();
			}
			else
			{
				string NewPath = path + @"/" + FileResource.Name;

				Directory.CreateDirectory(NewPath);
				List<File> SubFolderItems = IterateFolder(service, FileResource.Id);

				foreach (File Item in SubFolderItems)
				{
					downloadFile(service, Item, NewPath);
				}
			}
		}

		public List<File> IterateFolder(DriveService service, string folderId)
		{
			List<File> TList = new List<File>();
			FilesResource.ListRequest request = service.Files.List();
			request.Q = $"'{folderId}' in parents";

			do
			{
				try
				{
					FileList children = request.Execute();

					foreach (File child in children.Files)
						TList.Add(service.Files.Get(child.Id).Execute());

					request.PageToken = children.NextPageToken;
				}
				catch (Exception e)
				{
					Console.Write("An error occured:" + e.Message);
					request.PageToken = null;
				}
			} while (!string.IsNullOrEmpty(request.PageToken));

			return TList;
		}

		#endregion
	}
}