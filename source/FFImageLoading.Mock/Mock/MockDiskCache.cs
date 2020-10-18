using System;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Cache;
using System.Collections.Generic;

namespace FFImageLoading.Mock
{
    public class MockDiskCache : IDiskCache
    {
        readonly string _path = "/mock/ffimageloading";
        Dictionary<string, MockFile> _cache = new Dictionary<string, MockFile>();
        readonly object _lock = new object();

        public Task AddToSavingQueueIfNotExistsAsync(string key, byte[] bytes, TimeSpan duration,
	        string uri = null,
	        Action<FileWriteInfo> writeFinishedAction = null)
        {
            lock (_lock)
            {
                _cache.Add(key, new MockFile(bytes, Path.Combine(_path, key)));
                writeFinishedAction?.Invoke(new FileWriteInfo());
                return Task.FromResult(true);
            }
        }

        public Task ClearAsync()
        {
            lock (_lock)
            {
                _cache.Clear();
                return Task.FromResult(true);
            }
        }

        public Task<bool> ExistsAsync(string key)
        {
            lock (_lock)
            {
                return Task.FromResult(_cache.ContainsKey(key));
            }
        }

        public Task<string> GetFilePathAsync(string key)
        {
            lock (_lock)
            {
                return Task.FromResult(_cache[key].Path);
            }
        }

        public Task RemoveAsync(string key)
        {
            lock (_lock)
            {
                _cache.Remove(key);
                return Task.FromResult(true);
            }
        }

        public Task<Stream> TryGetStreamAsync(string key)
        {
            lock (_lock)
            {
                MockFile file;

                if (_cache.TryGetValue(key, out file))
                {
                    return Task.FromResult<Stream>(new MemoryStream(file.Data));
                }

                return Task.FromResult<Stream>(null);
            }
        }
    }
}
