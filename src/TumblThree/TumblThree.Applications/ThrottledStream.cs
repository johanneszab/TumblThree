using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Timers;
using TumblThree.Applications.Properties;

namespace TumblThree.Applications
{
    public class ThrottledStream : Stream
    {
        #region Properties

        private int maxBytesPerSecond;
        /// <summary>
        /// Number of Bytes that are allowed per second
        /// </summary>
        public int MaxBytesPerSecond
        {
            get { return maxBytesPerSecond; }
            set
            {
                if (value < 1)
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.MaxBytePerSecond));

                maxBytesPerSecond = value;
            }
        }

        #endregion


        #region Private Members

        private int processed;
        System.Timers.Timer resettimer;
        AutoResetEvent wh = new AutoResetEvent(true);
        private Stream parent;

        #endregion

        /// <summary>
        /// Creates a new Stream with Databandwith cap
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

        protected void Throttle(int bytes)
        {
            try
            {
                processed += bytes;
                if (processed >= maxBytesPerSecond)
                    wh.WaitOne();
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

        #region Stream-Overrides

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

        public override void Flush()
        {
            parent.Flush();
        }

        public override long Length
        {
            get { return parent.Length; }
        }

        public override long Position
        {
            get
            {
                return parent.Position;
            }
            set
            {
                parent.Position = value;
            }
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

        #endregion


        public static ThrottledStream ReadFromURLIntoStream(string url, int bandwidthInKb, int timeoutInSeconds, string proxyHost, string proxyPort)
        {
            // Create a web request to the URL
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            if (!String.IsNullOrEmpty(proxyHost))
            {
                request.Proxy = new WebProxy(proxyHost, Int32.Parse(proxyPort));
            }
            else
            {
                request.Proxy = null; // WebRequest.GetSystemWebProxy();
            }
            request.KeepAlive = true;
            request.AllowAutoRedirect = true;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Pipelined = true;
            request.Timeout = timeoutInSeconds * 1000;
            request.ServicePoint.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 400;
            //request.ContentLength = 0;
            //request.ContentType = "x-www-from-urlencoded";

            // Get the web response
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // Make sure the response is valid
            if (HttpStatusCode.OK == response.StatusCode)
            {
                // Open the response stream
                Stream responseStream = response.GetResponseStream();
                return new ThrottledStream(responseStream, bandwidthInKb * 1024);
            }
            else
            {
                return null;
            }
        }

        public static bool SaveStreamToDisk(Stream input, string destinationFileName)
        {

            // Open the destination file
            using (FileStream stream = new FileStream(destinationFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                // Create a 4K buffer to chunk the file
                byte[] buf = new byte[4096];
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