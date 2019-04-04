using FFImageLoading.Work;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using FFImageLoading.Extensions;

namespace FFImageLoading.DataResolvers
{
    public class ResourceDataResolver : IDataResolver
    {
        private static readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);
        private static Dictionary<string, StreamResourceInfo> _cache = new Dictionary<string, StreamResourceInfo>(128);

        public async virtual Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            StreamResourceInfo image = null;
            await _cacheLock.WaitAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            Uri imgUri=null;
            try
            {
                string resPath = identifier.TrimStart('\\', '/');

                if (!resPath.StartsWith(@"Assets\", StringComparison.OrdinalIgnoreCase) && !resPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    resPath = @"Assets\" + resPath;
                }

                imgUri = new Uri($"pack://application:,,,/{resPath}");
                var key = imgUri.ToString();
                if (!_cache.TryGetValue(key, out image))
                {
                    image = Application.GetResourceStream(imgUri);

                    //image = new BitmapImage(imgUri);

                    if (_cache.Count >= 128)
                        _cache.Clear();

                    _cache[key] = image;
                }
            }
            catch (Exception)
            {
                try
                {
                    imgUri = new Uri($"pack://application:,,,/{identifier}");
                    //imgUri = new Uri(identifier, UriKind.RelativeOrAbsolute);
                    var key = imgUri.ToString();
                    if (!_cache.TryGetValue(key, out image))
                    {
                        image = Application.GetResourceStream(imgUri);

                        if (_cache.Count >= 128)
                            _cache.Clear();

                        _cache[key] = image;
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

            if (image != null)
            {
                var imageInformation = new ImageInformation();
                imageInformation.SetPath(identifier);
                imageInformation.SetFilePath(imgUri.ToString());

                token.ThrowIfCancellationRequested();

                var s = await image.Stream.AsRandomAccessStream();
	            s.Seek(0, SeekOrigin.Begin);
                return new DataResolverResult(s, LoadingResult.CompiledResource, imageInformation);
            }

            throw new FileNotFoundException(identifier);
        }
    }
}
