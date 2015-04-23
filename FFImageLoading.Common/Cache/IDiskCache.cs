using System;
using System.Threading.Tasks;
using System.IO;

namespace FFImageLoading.Cache
{
    public interface IDiskCache
    {
        string BasePath { get; }

        Task AddOrUpdateAsync(string key, byte[] data, TimeSpan duration);

        Task<byte[]> TryGetAsync (string key);

		Stream TryGetStream (string key);
    }
}

