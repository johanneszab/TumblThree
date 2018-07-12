using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TumblThree.Applications.Extensions;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;

namespace TumblThree.Applications.Downloader
{
    public class FileDownloader
    {
        private readonly AppSettings settings;
        private readonly CancellationToken ct;
        private readonly IWebRequestFactory webRequestFactory;
        private readonly ISharedCookieService cookieService;
        public static readonly int BufferSize = 512 * 4096;
        public event EventHandler Completed;
        public event EventHandler<DownloadProgressChangedEventArgs> ProgressChanged;

        public FileDownloader(AppSettings settings, CancellationToken ct, IWebRequestFactory webRequestFactory, ISharedCookieService cookieService)
        {
            this.settings = settings;
            this.ct = ct;
            this.webRequestFactory = webRequestFactory;
            this.cookieService = cookieService;
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
                if (totalBytesReceived >= await CheckDownloadSizeAsync(url).TimeoutAfter(settings.TimeOut))
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
                        HttpWebRequest request = webRequestFactory.CreateGetReqeust(url);
                        requestRegistration = ct.Register(() => request.Abort());
                        request.AddRange(totalBytesReceived);

                        long totalBytesToReceive = 0;
                        using (WebResponse response = await request.GetResponseAsync().TimeoutAfter(settings.TimeOut))
                        {
                            totalBytesToReceive = totalBytesReceived + response.ContentLength;

                            using (Stream responseStream = response.GetResponseStream())
                            {
                                using (Stream throttledStream = GetStreamForDownload(responseStream))
                                {
                                    var buffer = new byte[4096];
                                    var bytesRead = 0;
                                    //Stopwatch sw = Stopwatch.StartNew();

                                    while ((bytesRead = await throttledStream.ReadAsync(buffer, 0, buffer.Length, ct).TimeoutAfter(settings.TimeOut)) > 0)
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

        private async Task<long> CheckDownloadSizeAsync(string url)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                HttpWebRequest request = webRequestFactory.CreateGetReqeust(url);
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

        public async Task<Stream> ReadFromUrlIntoStream(string url)
        {
            HttpWebRequest request = webRequestFactory.CreateGetReqeust(url);

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

        private Stream GetStreamForDownload(Stream stream)
        {
            if (settings.Bandwidth == 0)
                return stream;
            return new ThrottledStream(stream, (settings.Bandwidth / settings.ConcurrentConnections) * 1024);
        }

        public static async Task<bool> SaveStreamToDisk(Stream input, string destinationFileName, CancellationToken ct)
        {
            using (var stream = new FileStream(destinationFileName, FileMode.Create, FileAccess.Write, FileShare.Read, BufferSize, true))
            {
                var buf = new byte[4096];
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
