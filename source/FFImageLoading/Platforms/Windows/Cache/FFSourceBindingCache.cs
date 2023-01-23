using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace FFImageLoading.Cache
{
    /// <summary>
    /// This class optimizes the call to "StorageFile.GetFileFromPathAsync" that is time consuming.
    /// The source of each image is the key of the cache... once a source has been checked the first time, any other control can be skipped
    /// </summary>
    public static class FFSourceBindingCache
    {
        private static Dictionary<string, Tuple<bool, StorageFile>> _cache = new Dictionary<string, Tuple<bool, StorageFile>>(128);

        public static async Task<bool> IsFileAsync(string path)
        {

            if (_cache.ContainsKey(path))
            {
                return _cache[path].Item1;
            }
            else
            {
                if (_cache.Count >= 128)
                {
                    _cache.Clear();
                }

                StorageFile file = await GetFileAsync(path).ConfigureAwait(false);
                _cache.Add(path, new Tuple<bool, StorageFile>(file != null, file));
                return file != null;
            }
        }

        public static async Task<StorageFile> GetFileAsync(string path)
        {
            StorageFile file = null;
            try
            {
                var filePath = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(filePath) && !(filePath.TrimStart('\\', '/')).StartsWith("Assets"))
                {
                    file = await StorageFile.GetFileFromPathAsync(path);
                }
            }
            catch (Exception)
            {
            }

            return file;
        }
    }
}
