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
#if __MACOS__
        readonly string[] fileTypes = { null, "png", "jpg", "jpeg", "PNG", "JPG", "JPEG" };
#elif __IOS__
        readonly string[] fileTypes = { null, "png", "jpg", "jpeg", "PNG", "JPG", "JPEG", "webp", "WEBP" };
#endif
        public virtual async Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            NSBundle bundle = null;
            var filename = Path.GetFileNameWithoutExtension(identifier);
            var tmpPath = Path.GetDirectoryName(identifier).Trim('/');
            var filenamePath = string.IsNullOrWhiteSpace(tmpPath) ? null : tmpPath + "/";

            foreach (var fileType in fileTypes)
            {
                string file = null;
                var extension = Path.HasExtension(identifier) ? Path.GetExtension(identifier) : string.IsNullOrWhiteSpace(fileType) ? string.Empty : "." + fileType;

                token.ThrowIfCancellationRequested();

                int scale = (int)ScaleHelper.Scale;
                if (scale > 1)
                {
                    while (scale > 1)
                    {
                        token.ThrowIfCancellationRequested();

                        var tmpFile = string.Format("{0}@{1}x{2}", filename, scale, extension);
                        bundle = NSBundle._AllBundles.FirstOrDefault(bu =>
                        {
                            var path = string.IsNullOrWhiteSpace(filenamePath) ?
                                             bu.PathForResource(tmpFile, null) :
                                             bu.PathForResource(tmpFile, null, filenamePath);
                            return !string.IsNullOrWhiteSpace(path);
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
                    var tmpFile = string.Format(filename + extension);
                    file = tmpFile;
                    bundle = NSBundle._AllBundles.FirstOrDefault(bu =>
                    {
                        var path = string.IsNullOrWhiteSpace(filenamePath) ?
                                         bu.PathForResource(tmpFile, null) :
                                         bu.PathForResource(tmpFile, null, filenamePath);

                        return !string.IsNullOrWhiteSpace(path);
                    });
                }

                token.ThrowIfCancellationRequested();

                if (bundle != null)
                {
                    string path = !string.IsNullOrEmpty(filenamePath) ? bundle.PathForResource(file, null, filenamePath) : bundle.PathForResource(file, null);

                    var stream = FileStore.GetInputStream(path, true);
                    var imageInformation = new ImageInformation();
                    imageInformation.SetPath(identifier);
                    imageInformation.SetFilePath(path);

                    return new Tuple<Stream, LoadingResult, ImageInformation>(
                        stream, LoadingResult.CompiledResource, imageInformation);
                }

                token.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(fileType))
                {
                    //Asset catalog
                    bool tryAssetsCatalog = true;
#if __IOS__
                    if (!UIDevice.CurrentDevice.CheckSystemVersion(9, 0)) tryAssetsCatalog = false;
#endif
                    if (tryAssetsCatalog)
                    {

                        NSDataAsset asset = null;

                        try
                        {
                            await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() => asset = new NSDataAsset(filename)).ConfigureAwait(false);
                        }
                        catch (Exception) { }

                        if (asset != null)
                        {
                            token.ThrowIfCancellationRequested();
                            var stream = asset.Data?.AsStream();
                            var imageInformation = new ImageInformation();
                            imageInformation.SetPath(identifier);
                            imageInformation.SetFilePath(null);

                            return new Tuple<Stream, LoadingResult, ImageInformation>(
                                stream, LoadingResult.CompiledResource, imageInformation);
                        }
                    }

                    PImage image = null;

                    try
                    {
#if __IOS__
                        await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() => image = PImage.FromBundle(filename)).ConfigureAwait(false);
#elif __MACOS__
                        await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() => image = NSImage.ImageNamed(filename)).ConfigureAwait(false);
#endif
                    }
                    catch (Exception) { }

                    if (image != null)
                    {
                        token.ThrowIfCancellationRequested();

                        var stream = image.AsPngStream();

                        var imageInformation = new ImageInformation();
                        imageInformation.SetPath(identifier);
                        imageInformation.SetFilePath(null);

                        return new Tuple<Stream, LoadingResult, ImageInformation>(
                            stream, LoadingResult.CompiledResource, imageInformation);
                    }
                }
            }

            throw new FileNotFoundException(identifier);
        }
    }
}
