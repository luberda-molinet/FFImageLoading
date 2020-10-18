using FFImageLoading.Work;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;
using System.Collections.Generic;

namespace FFImageLoading.DataResolvers
{
    public class ResourceDataResolver : IDataResolver
    {
        private static readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);
        private static Dictionary<string, StorageFile> _cache = new Dictionary<string, StorageFile>(128);

        public async virtual Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            StorageFile file = null;
            await _cacheLock.WaitAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            try
            {
                string resPath = identifier.TrimStart('\\', '/');

                if (!resPath.StartsWith(@"Assets\") && !resPath.StartsWith("Assets/"))
                {
                    resPath = @"Assets\" + resPath;
                }

                var imgUri = new Uri("ms-appx:///" + resPath);
                var key = imgUri.ToString();
                if (!_cache.TryGetValue(key, out file))
                {
                    file = await StorageFile.GetFileFromApplicationUriAsync(imgUri);

                    if (_cache.Count >= 128)
                        _cache.Clear();

                    _cache[key] = file;
                }
            }
            catch (Exception)
            {
                try
                {
                    var imgUri = new Uri("ms-appx:///" + identifier);
                    var key = imgUri.ToString();
                    if (!_cache.TryGetValue(key, out file))
                    {
                        file = await StorageFile.GetFileFromApplicationUriAsync(imgUri);

                        if (_cache.Count >= 128)
                            _cache.Clear();

                        _cache[key] = file;
                    }
                }
                catch (Exception)
                {
                }
            }
            finally
            {
                _cacheLock.Release();
            }

            if (file != null)
            {
                var imageInformation = new ImageInformation();
                imageInformation.SetPath(identifier);
                imageInformation.SetFilePath(file.Path);

                token.ThrowIfCancellationRequested();
                var stream = await file.OpenStreamForReadAsync().ConfigureAwait(false);

                return new DataResolverResult(stream, LoadingResult.CompiledResource, imageInformation);
            }

            throw new FileNotFoundException(identifier);
        }
    }
}
