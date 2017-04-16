using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using TumblThree.Applications.Properties;

namespace TumblThree.Applications
{
    public class ThrottledStream : Stream
    {
        private readonly Stream parent;
        readonly System.Timers.Timer resettimer;
        readonly AutoResetEvent wh = new AutoResetEvent(true);

        private int maxBytesPerSecond;

        private int processed;

        /// <summary>
        ///     Creates a new Stream with Databandwith cap
        /// </summary>
        /// <param name="parentStream"></param>
        /// <param name="maxBytesPerSecond"></param>
        public ThrottledStream(Stream parentStream, int maxBytesPerSecond = int.MaxValue)
        {
            MaxBytesPerSecond = maxBytesPerSecond;
            parent = parentStream;
            processed = 0;
            resettimer = new System.Timers.Timer();
            resettimer.Interval = 1000;
            resettimer.Elapsed += resettimer_Elapsed;
            resettimer.Start();
        }

        /// <summary>
        ///     Number of Bytes that are allowed per second
        /// </summary>
        private int MaxBytesPerSecond
        {
            get { return maxBytesPerSecond; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.MaxBytePerSecond));
                }

                maxBytesPerSecond = value;
            }
        }

        public override bool CanRead
        {
            get { return parent.CanRead; }
        }

        public override bool CanSeek
        {
            get { return parent.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return parent.CanWrite; }
        }

        public override long Length
        {
            get { return parent.Length; }
        }

        public override long Position
        {
            get { return parent.Position; }
            set { parent.Position = value; }
        }

        private void Throttle(int bytes)
        {
            try
            {
                processed += bytes;
                if (processed >= maxBytesPerSecond)
                {
                    wh.WaitOne();
                }
            }
            catch
            {
            }
        }

        private void resettimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            processed = 0;
            wh.Set();
        }

        public override void Close()
        {
            resettimer.Stop();
            resettimer.Close();
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            resettimer.Dispose();
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            parent.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Throttle(count);
            return parent.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return parent.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            parent.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Throttle(count);
            parent.Write(buffer, offset, count);
        }

        public static async Task<ThrottledStream> ReadFromURLIntoStream(string url, AppSettings settings)
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
            request.ReadWriteTimeout = settings.TimeOut * 1000;
            request.Timeout = -1;
            ServicePointManager.DefaultConnectionLimit = 400;
            if (!string.IsNullOrEmpty(settings.ProxyHost))
            {
                request.Proxy = new WebProxy(settings.ProxyHost, int.Parse(settings.ProxyPort));
            }
            else
            {
                request.Proxy = null;
            }
            var response = await request.GetResponseAsync() as HttpWebResponse;
            if (HttpStatusCode.OK == response.StatusCode)
            {
                Stream responseStream = response.GetResponseStream();
                return new ThrottledStream(responseStream, settings.Bandwidth * 1024);
            }
            else
            {
                return null;
            }
        }

        private static async Task<long> CheckDownloadSizeAsync(string url, AppSettings settings, CancellationToken ct)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ProtocolVersion = HttpVersion.Version11;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 |
                                                   SecurityProtocolType.Tls;
            request.UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
            request.AllowAutoRedirect = true;
            request.KeepAlive = true;
            request.Pipelined = true;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ReadWriteTimeout = settings.TimeOut * 1000;
            request.Timeout = -1;
            ct.Register(() => request.Abort());
            ServicePointManager.DefaultConnectionLimit = 400;
            if (!string.IsNullOrEmpty(settings.ProxyHost))
            {
                request.Proxy = new WebProxy(settings.ProxyHost, int.Parse(settings.ProxyPort));
            }
            else
            {
                request.Proxy = null;
            }

            using (WebResponse response = await request.GetResponseAsync())
            {
                return response.ContentLength;
            }
        }

        // FIXME: Needs a complete rewrite. Also a append/cache function for resuming incomplete files on the disk.
        // Should be in separated class with support for events for downloadspeed, is resumable file?, etc.
        // Should check if file is complete, else it will trigger an WebException -- 416 requested range not satisfiable at every request 
        public static async Task<bool> DownloadFileWithResumeAsync(string url, string destinationPath, AppSettings settings, CancellationToken ct)
        {
            long totalBytesReceived = 0;
            var attemptCount = 0;

            if (File.Exists(destinationPath))
            {
                var fileInfo = new FileInfo(destinationPath);
                totalBytesReceived = fileInfo.Length;
				if (totalBytesReceived >= await CheckDownloadSizeAsync(url, settings, ct))
                    return true;
            }
            FileMode fileMode = totalBytesReceived > 0 ? FileMode.Append : FileMode.Create;

            using (FileStream fileStream = File.Open(destinationPath, fileMode, FileAccess.Write, FileShare.Read))
            {
                while (true)
                {
                    attemptCount += 1;

                    if (attemptCount > settings.MaxNumberOfRetries)
                    {
                        return false;
                    }

                    try
                    {
                        var request = (HttpWebRequest)WebRequest.Create(url);
                        request.Method = "GET";
                        request.ProtocolVersion = HttpVersion.Version11;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 |
                                                               SecurityProtocolType.Tls;
                        request.UserAgent =
                            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                        request.AllowAutoRedirect = true;
                        request.KeepAlive = true;
                        request.Pipelined = true;
                        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                        request.ReadWriteTimeout = settings.TimeOut * 1000;
                        request.Timeout = -1;
                        ct.Register(() => request.Abort());
                        ServicePointManager.DefaultConnectionLimit = 400;
                        if (!string.IsNullOrEmpty(settings.ProxyHost))
                        {
                            request.Proxy = new WebProxy(settings.ProxyHost, int.Parse(settings.ProxyPort));
                        }
                        else
                        {
                            request.Proxy = null;
                        }

                        request.AddRange(totalBytesReceived);

                        long totalBytesToReceive = 0;
                        using (WebResponse response = await request.GetResponseAsync())
                        {
                            totalBytesToReceive = totalBytesReceived + response.ContentLength;

                            using (Stream responseStream = response.GetResponseStream())
                            {
                                using (var throttledStream = new ThrottledStream(responseStream, settings.Bandwidth * 1024))
                                {
                                    var buffer = new byte[4096];
                                    int bytesRead = throttledStream.Read(buffer, 0, buffer.Length);
                                    while (bytesRead > 0)
                                    {
                                        fileStream.Write(buffer, 0, bytesRead);
                                        totalBytesReceived += bytesRead;
                                        bytesRead = throttledStream.Read(buffer, 0, buffer.Length);
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
                }
                return true;
            }
        }

        public static bool SaveStreamToDisk(Stream input, string destinationFileName)
        {
            // Open the destination file
            using (var stream = new FileStream(destinationFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                // Create a 4K buffer to chunk the file
                var buf = new byte[4096];
                int BytesRead;
                // Read the chunk of the web response into the buffer
                while (0 < (BytesRead = input.Read(buf, 0, buf.Length)))
                {
                    // Write the chunk from the buffer to the file   
                    stream.Write(buf, 0, BytesRead);
                }
            }
            return true;
        }
    }
}
