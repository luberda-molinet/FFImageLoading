using System;
using System.IO;

namespace FFImageLoading.Extensions
{
	public static class StreamExtensions
	{
		public static byte[] ToBytes(this Stream sourceStream)
		{
			using (var memoryStream = new MemoryStream())
			{
				sourceStream.CopyTo(memoryStream);
				return memoryStream.ToArray();
			}
		}
	}
}
