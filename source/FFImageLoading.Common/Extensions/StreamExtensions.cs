using System;
using System.IO;
using System.Threading.Tasks;

namespace FFImageLoading
{
	public static class StreamExtensions
	{
		public static async Task<Stream> AsSeekableStreamAsync(this Stream stream, bool forceCopy = false)
		{
			if (stream == null)
				return null;

			var lengthSupported = true;

			try
			{
				var length = stream.Length;
			}
			catch (NotSupportedException)
			{
				lengthSupported = false;
			}

			if (lengthSupported && !forceCopy && stream.CanSeek)
			{
				if (stream.Position != 0)
					stream.Position = 0;

				return stream;
			}

			using (stream)
			{
				var ms = new MemoryStream();
				await stream.CopyToAsync(ms).ConfigureAwait(false);
				ms.Position = 0;
				return ms;
			}
		}

		public static byte[] ToByteArray(this Stream stream)
		{
			if (stream == null)
				return null;

			if (stream is MemoryStream memoryStream)
			{
				return memoryStream.ToArray();
			}

			using (var ms = new MemoryStream())
			{
				stream.CopyTo(ms);
				return ms.ToArray();
			}
		}
	}
}
