using System;
using FFImageLoading.Work;
using System.IO;
using System.Threading.Tasks;
using Foundation;
using System.Linq;
using FFImageLoading.IO;
using System.Threading;
using FFImageLoading.Helpers;
using UIKit;

namespace FFImageLoading.DataResolvers
{
    public class BundleDataResolver : IDataResolver
	{
        readonly string[] fileTypes = { null, "png", "jpg", "jpeg", "PNG", "JPG", "JPEG","webp", "WEBP"};

        public virtual async Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            NSBundle bundle = null;
            string file = null;
            var filename = Path.GetFileNameWithoutExtension(identifier);
            var extension = Path.GetExtension(identifier);
            const string pattern = "{0}@{1}x{2}";

            foreach (var fileType in fileTypes)
            {
                int scale = (int)ScaleHelper.Scale;
                if (scale > 1)
                {
                    while (scale > 1)
                    {
                        var tmpFile = string.Format(pattern, filename, scale, extension);
                        bundle = NSBundle._AllBundles.FirstOrDefault(bu =>
                        {
                            var path = bu.PathForResource(tmpFile, fileType);
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

                if (bundle == null)
                {
                    file = identifier;
                    bundle = NSBundle._AllBundles.FirstOrDefault(bu =>
                    {
                        var path = bu.PathForResource(file, fileType);
                        return !string.IsNullOrWhiteSpace(path);
                    });
                }

                if (bundle != null)
                {
                    var path = bundle.PathForResource(file, fileType);

                    var stream = FileStore.GetInputStream(path, true);
                    var imageInformation = new ImageInformation();
                    imageInformation.SetPath(identifier);
                    imageInformation.SetFilePath(path);

                    return new Tuple<Stream, LoadingResult, ImageInformation>(
                        stream, LoadingResult.CompiledResource, imageInformation);
                }
            }

            //Asset catalog

            if (UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
            {
                NSDataAsset asset = null;

                try
                {
                    await MainThreadDispatcher.Instance.PostAsync(() => asset = new NSDataAsset(filename)).ConfigureAwait(false);
                }
                catch (Exception) { }

                if (asset != null)
                {
                    var stream = asset.Data?.AsStream();
                    var imageInformation = new ImageInformation();
                    imageInformation.SetPath(identifier);
                    imageInformation.SetFilePath(null);

                    return new Tuple<Stream, LoadingResult, ImageInformation>(
                        stream, LoadingResult.CompiledResource, imageInformation);
                }
            }
            else if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
            {
                UIImage image = null;

                try
                {
                    await MainThreadDispatcher.Instance.PostAsync(() => image = UIImage.FromBundle(filename)).ConfigureAwait(false);
                }
                catch (Exception) { }

                if (image != null)
                {
                    var stream = image.AsPNG()?.AsStream();
                    var imageInformation = new ImageInformation();
                    imageInformation.SetPath(identifier);
                    imageInformation.SetFilePath(null);

                    return new Tuple<Stream, LoadingResult, ImageInformation>(
                        stream, LoadingResult.CompiledResource, imageInformation);
                }
            }

            throw new FileNotFoundException(identifier);
        }
    }
}