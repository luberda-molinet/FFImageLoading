using System;
using System.Threading.Tasks;

namespace FFImageLoading.Cache
{
    public interface IDiskCache
    {
        string BasePath { get; }

        Task AddOrUpdateAsync(string key, byte[] data, TimeSpan duration);

        Task<byte[]> TryGetAsync (string key);
    }
}

