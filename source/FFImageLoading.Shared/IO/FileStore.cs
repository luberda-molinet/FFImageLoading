using System;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace FFImageLoading.IO
{
    internal static class FileStore
    {
		public static Stream GetInputStream(string path)
		{
			return new FileStream(path, FileMode.Open, FileAccess.Read);
		}

		public static Stream GetOutputStream(string path)
		{
			return new FileStream(path, FileMode.Create, FileAccess.Write);
		}

		public static bool Exists(string path)
		{
			return File.Exists(path);
		}

        public static async Task<byte[]> ReadBytesAsync(string path, CancellationToken token)
        {
			using (var fs = GetInputStream(path))
			{
				var buff = new byte[fs.Length];
				await fs.ReadAsync(buff, 0, (int)fs.Length, token).ConfigureAwait(false);
				return buff;
            }
        }

		public static async Task WriteBytesAsync(string path, byte[] data, CancellationToken token)
        {
            using (var fs = GetOutputStream(path)) {
				await fs.WriteAsync(data, 0, data.Length, token).ConfigureAwait(false);
            }
        }
    }
}

