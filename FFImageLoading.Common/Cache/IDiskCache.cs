using System;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace FFImageLoading.Cache
{
    public interface IDiskCache
    {
        string BasePath { get; }

		Task AddOrUpdateAsync (string key, Stream stream, CancellationToken token, TimeSpan duration);

		Task<byte[]> TryGetAsync (string key, CancellationToken token);

		Task<Stream> TryGetStream (string key);

		void Remove (string key);

		void Clear();
    }
}

