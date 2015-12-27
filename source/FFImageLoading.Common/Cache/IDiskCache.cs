using System;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace FFImageLoading.Cache
{
    public interface IDiskCache
    {
		Task<string> GetBasePathAsync();

		void AddToSavingQueueIfNotExists(string key, byte[] bytes, TimeSpan duration);

		Task<byte[]> TryGetAsync(string key, CancellationToken token);

		Task<Stream> TryGetStreamAsync(string key);

		Task RemoveAsync(string key);

		Task ClearAsync();
    }
}

