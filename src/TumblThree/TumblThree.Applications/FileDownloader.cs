using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;

namespace TumblThree.Applications
{
    public class FileDownloader
    {
        private readonly AppSettings settings;
        private readonly CancellationToken ct;
        private readonly ISharedCookieService cookieService;
        public static readonly int BufferSize = 512 * 4096;
        public event EventHandler Completed;
        public event EventHandler<DownloadProgressChangedEventArgs> ProgressChanged;

        public FileDownloader(AppSettings settings, CancellationToken ct, ISharedCookieService cookieService)
        {
            this.settings = settings;
            this.ct = ct;
            this.cookieService = cookieService;
        }

        private HttpWebRequest CreateWebReqeust(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ProtocolVersion = HttpVersion.Version11;
            request.UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
            request.AllowAutoRedirect = true;
            request.KeepAlive = true;
            request.Pipelined = true;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            // Timeouts don't work with GetResponseAsync() as it internally uses BeginGetResponse.
            // See docs: https://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.timeout(v=vs.110).aspx
            // Quote: The Timeout property has no effect on asynchronous requests made with the BeginGetResponse or BeginGetRequestStream method.
            // TODO: Use HttpClient instead?
            request.ReadWriteTimeout = settings.TimeOut * 1000;
            request.Timeout = settings.TimeOut * 1000;
            request.CookieContainer = new CookieContainer();
            //TODO: Fix site specific cookies!
            cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
            ServicePointManager.DefaultConnectionLimit = 400;
            request = SetWebRequestProxy(request, settings);
            return request;
        }

        private static HttpWebRequest SetWebRequestProxy(HttpWebRequest request, AppSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.ProxyHost) && !string.IsNullOrEmpty(settings.ProxyPort))
                request.Proxy = new WebProxy(settings.ProxyHost, int.Parse(settings.ProxyPort));
            else
                request.Proxy = null;

            if (!string.IsNullOrEmpty(settings.ProxyUsername) && !string.IsNullOrEmpty(settings.ProxyPassword))
                request.Proxy.Credentials = new NetworkCredential(settings.ProxyUsername, settings.ProxyPassword);
            return request;
        }

        public async Task<Stream> ReadFromUrlIntoStream(string url)
        {
            HttpWebRequest request = CreateWebReqeust(url);

            using (var response = await request.GetResponseAsync() as HttpWebResponse)
            {
                if (HttpStatusCode.OK == response.StatusCode)
                {
                    Stream responseStream = response.GetResponseStream();
                    return GetStreamForDownload(responseStream);
                }
                else
                {
                    return null;
                }
            }
        }

        private async Task<long> CheckDownloadSizeAsync(string url)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                HttpWebRequest request = CreateWebReqeust(url);
                requestRegistration = ct.Register(() => request.Abort());

                using (WebResponse response = await request.GetResponseAsync())
                {
                    return response.ContentLength;
                }
            }
            finally
            {
                requestRegistration.Dispose();
            }

        }

        protected Stream GetStreamForDownload(Stream stream)
        {
            if (settings.Bandwidth == 0)
                return stream;
            return new ThrottledStream(stream, (settings.Bandwidth / settings.ParallelImages) * 1024);
        }

        // TODO: Needs a complete rewrite. Also a append/cache function for resuming incomplete files on the disk.
        // Should be in separated class with support for events for downloadspeed, is resumable file?, etc.
        // Should check if file is complete, else it will trigger an WebException -- 416 requested range not satisfiable at every request 
        public async Task<bool> DownloadFileWithResumeAsync(string url, string destinationPath)
        {
            long totalBytesReceived = 0;
            var attemptCount = 0;
            int bufferSize = settings.BufferSize * 4096;

            if (File.Exists(destinationPath))
            {
                var fileInfo = new FileInfo(destinationPath);
                totalBytesReceived = fileInfo.Length;
                if (totalBytesReceived >= await CheckDownloadSizeAsync(url))
                    return true;
            }
            if (ct.IsCancellationRequested)
                return false;

            FileMode fileMode = totalBytesReceived > 0 ? FileMode.Append : FileMode.Create;

            using (var fileStream = new FileStream(destinationPath, fileMode, FileAccess.Write, FileShare.Read, bufferSize, true))
            {
                while (true)
                {
                    attemptCount += 1;

                    if (attemptCount > settings.MaxNumberOfRetries)
                    {
                        return false;
                    }

                    var requestRegistration = new CancellationTokenRegistration();

                    try
                    {
                        HttpWebRequest request = CreateWebReqeust(url);
                        requestRegistration = ct.Register(() => request.Abort());
                        request.AddRange(totalBytesReceived);

                        long totalBytesToReceive = 0;
                        using (WebResponse response = await request.GetResponseAsync())
                        {
                            totalBytesToReceive = totalBytesReceived + response.ContentLength;

                            using (Stream responseStream = response.GetResponseStream())
                            {
                                using (Stream throttledStream = GetStreamForDownload(responseStream))
                                {
                                    var buffer = new byte[bufferSize];
                                    var bytesRead = 0;
                                    //Stopwatch sw = Stopwatch.StartNew();

                                    while ((bytesRead = await throttledStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                                    {
                                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                                        totalBytesReceived += bytesRead;

                                        //float currentSpeed = totalBytesReceived / (float)sw.Elapsed.TotalSeconds;
                                        //OnProgressChanged(new DownloadProgressChangedEventArgs(totalBytesReceived,
                                        //    totalBytesToReceive, (long)currentSpeed));
                                    }
                                }
                            }
                        }
                        if (totalBytesReceived >= totalBytesToReceive)
                        {
                            break;
                        }
                    }
                    catch (IOException ioException)
                    {
                        // file in use
                        long win32ErrorCode = ioException.HResult & 0xFFFF;
                        if (win32ErrorCode == 0x21 || win32ErrorCode == 0x20)
                        {
                            return false;
                        }
                        // retry (IOException: Received an unexpected EOF or 0 bytes from the transport stream)
                    }
                    catch (WebException webException)
                    {
                        if (webException.Status == WebExceptionStatus.ConnectionClosed)
                        {
                            // retry
                        }
                        else
                        {
                            throw;
                        }
                    }
                    finally
                    {
                        requestRegistration.Dispose();
                    }
                }
                return true;
            }
        }

        public static async Task<bool> SaveStreamToDisk(Stream input, string destinationFileName, CancellationToken ct)
        {
            using (var stream = new FileStream(destinationFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                var buf = new byte[BufferSize];
                int bytesRead;
                while (0 < (bytesRead = await input.ReadAsync(buf, 0, buf.Length, ct)))
                {
                    await stream.WriteAsync(buf, 0, bytesRead, ct);
                }
            }
            return true;
        }

        protected void OnProgressChanged(DownloadProgressChangedEventArgs e)
        {
            EventHandler<DownloadProgressChangedEventArgs> handler = ProgressChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnCompleted(EventArgs e)
        {
            EventHandler handler = Completed;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }

    public class DownloadProgressChangedEventArgs : EventArgs
    {
        public DownloadProgressChangedEventArgs(long totalReceived, long fileSize, long currentSpeed)
        {
            BytesReceived = totalReceived;
            TotalBytesToReceive = fileSize;
            CurrentSpeed = currentSpeed;
        }

        public long BytesReceived { get; private set; }
        public long TotalBytesToReceive { get; private set; }
        public float ProgressPercentage
        {
            get
            {
                return ((float)BytesReceived / (float)TotalBytesToReceive) * 100;
            }
        }
        public float CurrentSpeed { get; private set; } // in bytes
        public TimeSpan TimeLeft
        {
            get
            {
                long bytesRemainingtoBeReceived = TotalBytesToReceive - BytesReceived;
                return TimeSpan.FromSeconds(bytesRemainingtoBeReceived / CurrentSpeed);
            }
        }
    }
}
