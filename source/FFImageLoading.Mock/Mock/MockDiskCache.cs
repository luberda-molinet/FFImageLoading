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

        public Task AddToSavingQueueIfNotExistsAsync(string key, byte[] bytes, TimeSpan duration, Action writeFinished = null)
        {
            _cache.Add(key, new MockFile(bytes, Path.Combine(_path, key)));
            writeFinished?.Invoke();
            return Task.FromResult(true);
        }

        public Task ClearAsync()
        {
            _cache.Clear();
            return Task.FromResult(true);
        }

        public Task<bool> ExistsAsync(string key)
        {
            return Task.FromResult(_cache.ContainsKey(key));
        }

        public Task<string> GetFilePathAsync(string key)
        {
            return Task.FromResult(_cache[key].Path);
        }

        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            return Task.FromResult(true);
        }

        public Task<Stream> TryGetStreamAsync(string key)
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
