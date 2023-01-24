using System;
using FFImageLoading.Work;
using System.IO;
using System.Threading.Tasks;
using Foundation;
using System.Linq;
using FFImageLoading.IO;
using FFImageLoading.Extensions;
using System.Threading;
using FFImageLoading.Helpers;

#if __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
#elif __IOS__
using UIKit;
using PImage = UIKit.UIImage;
#endif


namespace FFImageLoading.DataResolvers
{
    public class BundleDataResolver : IDataResolver
    {
        private readonly string[] _fileTypes = { null, "png", "jpg", "jpeg", "webp", "gif" };

        public virtual async Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            var fileName = Path.GetFileNameWithoutExtension(identifier);

            var result = await ResolveFromBundlesAsync(fileName, identifier, parameters, token).ConfigureAwait(false);
            if (result != null)
                return result;

            token.ThrowIfCancellationRequested();

            result = await ResolveFromNamedResourceAsync(fileName, identifier, parameters, token).ConfigureAwait(false);
            if (result != null)
                return result;

            token.ThrowIfCancellationRequested();

            result = await ResolveFromAssetCatalogAsync(fileName, identifier, parameters, token).ConfigureAwait(false);
            if (result != null)
                return result;

            throw new FileNotFoundException(identifier);
        }

        private Task<DataResolverResult> ResolveFromBundlesAsync(string fileName, string identifier, TaskParameter parameters, CancellationToken token)
        {
            NSBundle bundle = null;
            var ext = Path.GetExtension(identifier)?.TrimStart(new char[] { '.' });
            var tmpPath = Path.GetDirectoryName(identifier)?.Trim('/');
            var filenamePath = string.IsNullOrWhiteSpace(tmpPath) ? null : tmpPath + "/";
            var hasExtension = !string.IsNullOrWhiteSpace(ext);

            var fileTypes = hasExtension ? new[] { ext } : _fileTypes;

            foreach (var fileType in fileTypes)
            {
                string file = null;
                string lastPath = null;

                token.ThrowIfCancellationRequested();

                var scale = (int)ScaleHelper.Scale;
                if (scale > 1)
                {
                    while (scale > 1)
                    {
                        token.ThrowIfCancellationRequested();

                        var tmpFile = string.Format("{0}@{1}x", fileName, scale);
                        bundle = NSBundle._AllBundles.FirstOrDefault(bu =>
                        {
                            lastPath = string.IsNullOrWhiteSpace(filenamePath) ?
                                             bu.PathForResource(tmpFile, fileType) :
                                             bu.PathForResource(tmpFile, fileType, filenamePath);

                            return !string.IsNullOrWhiteSpace(lastPath);
                        });

                        if (bundle != null)
                        {
                            file = tmpFile;
                            break;
                        }

                        scale--;
                    }
                }

                token.ThrowIfCancellationRequested();

                if (file == null)
                {
                    bundle = NSBundle._AllBundles.FirstOrDefault(bu =>
                    {
                        lastPath = string.IsNullOrWhiteSpace(filenamePath) ?
                                         bu.PathForResource(fileName, fileType) :
                                         bu.PathForResource(fileName, fileType, filenamePath);

                        return !string.IsNullOrWhiteSpace(lastPath);
                    });
                }

                token.ThrowIfCancellationRequested();

                if (bundle != null)
                {
                    var stream = FileStore.GetInputStream(lastPath, true);
                    var imageInformation = new ImageInformation();
                    imageInformation.SetPath(identifier);
                    imageInformation.SetFilePath(lastPath);
                    var result = new DataResolverResult(stream, LoadingResult.CompiledResource, imageInformation);

                    return Task.FromResult(result);
                }
            }

            return Task.FromResult<DataResolverResult>(null);
        }

        private async Task<DataResolverResult> ResolveFromAssetCatalogAsync(string fileName, string identifier, TaskParameter parameters, CancellationToken token)
        {
#if __IOS__
            if (!UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
                return null;
#endif

            NSDataAsset asset = null;

            try
            {
                await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() => asset = new NSDataAsset(identifier, NSBundle.MainBundle)).ConfigureAwait(false);
            }
            catch { }

            if (asset == null && fileName != identifier)
            {
                try
                {
                    await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() => asset = new NSDataAsset(fileName, NSBundle.MainBundle)).ConfigureAwait(false);
                }
                catch { }
            }

            if (asset != null)
            {
                token.ThrowIfCancellationRequested();
                var stream = asset.Data?.AsStream();
                var imageInformation = new ImageInformation();
                imageInformation.SetPath(identifier);
                imageInformation.SetFilePath(null);

                return new DataResolverResult(stream, LoadingResult.CompiledResource, imageInformation);
            }

            return null;
        }

        private async Task<DataResolverResult> ResolveFromNamedResourceAsync(string fileName, string identifier, TaskParameter parameters, CancellationToken token)
        {
            PImage image = null;

            try
            {
#if __IOS__
                await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() => image = PImage.FromBundle(identifier)).ConfigureAwait(false);
#elif __MACOS__
                await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() => image = PImage.ImageNamed(identifier)).ConfigureAwait(false);
#endif
            }
            catch { }

            if (image == null && fileName != identifier)
            {
                try
                {
#if __IOS__
                    await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() => image = PImage.FromBundle(fileName)).ConfigureAwait(false);
#elif __MACOS__
					await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() => image = PImage.ImageNamed(fileName)).ConfigureAwait(false);
#endif
                }
                catch { }
            }

            if (image != null)
            {
                token.ThrowIfCancellationRequested();

                var imageInformation = new ImageInformation();
                imageInformation.SetPath(identifier);
                imageInformation.SetFilePath(null);

                var container = new DecodedImage<object>()
                {
                    Image = image
                };

                var result = new DataResolverResult(container, LoadingResult.CompiledResource, imageInformation);

                return result;
            }

            return null;
        }
    }
}
