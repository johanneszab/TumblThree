using System.IO;

public class ProgressionStream:Stream
	{
		public delegate void ProgressionHandler(double progression);

		private Stream _sourceStream;
		private ProgressionHandler _progressionHandler;

		public ProgressionStream(Stream sourceStream, ProgressionHandler progressionHandler)
		{
			_sourceStream = sourceStream;
			_progressionHandler = progressionHandler;
		}

		public override int Read(byte[] array, int offset, int count)
		{
			_progressionHandler(Position / (double)Length * 100);

			return _sourceStream.Read(array, offset, count);
		}

		public override bool CanRead => _sourceStream.CanRead;

		public override bool CanSeek => _sourceStream.CanSeek;

		public override bool CanWrite => _sourceStream.CanWrite;

		public override long Length => _sourceStream.Length;

		public override long Position
		{
			get
			{
				return _sourceStream.Position;
			}

			set
			{
				_sourceStream.Position = value;
			}
		}

		public override void Flush()
		{
			_sourceStream.Flush();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _sourceStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_sourceStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_sourceStream.Write(buffer, offset, count);
		}
	}

