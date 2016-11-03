using System;
using FFImageLoading.Work;
using System.IO;
using System.Threading.Tasks;
using Foundation;
using System.Linq;
using FFImageLoading.IO;
using System.Threading;
using FFImageLoading.Helpers;

namespace FFImageLoading.DataResolvers
{
    public class BundleDataResolver : IDataResolver
	{
        readonly string[] fileTypes = { null, "png", "jpg", "jpeg", "PNG", "JPG", "JPEG","webp", "WEBP"};

        public virtual Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            NSBundle bundle = null;
            string file = null;

            foreach (var fileType in fileTypes)
            {
                int scale = (int)ScaleHelper.Scale;
                if (scale > 1)
                {
                    var filename = Path.GetFileNameWithoutExtension(identifier);
                    var extension = Path.GetExtension(identifier);
                    const string pattern = "{0}@{1}x{2}";

                    while (scale > 1)
                    {
                        var tmpFile = string.Format(pattern, filename, scale, extension);
                        bundle = NSBundle._AllBundles.FirstOrDefault(bu => !string.IsNullOrEmpty(bu.PathForResource(tmpFile, fileType)));

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
                    bundle = NSBundle._AllBundles.FirstOrDefault(bu => !string.IsNullOrEmpty(bu.PathForResource(identifier, fileType)));
                }

                if (bundle != null)
                {
                    var path = bundle.PathForResource(file, fileType);
                    var stream = FileStore.GetInputStream(path, true);
                    System.Diagnostics.Debug.WriteLine(path);
                    var imageInformation = new ImageInformation();
                    imageInformation.SetPath(identifier);
                    imageInformation.SetFilePath(path);

                    return Task.FromResult(new Tuple<Stream, LoadingResult, ImageInformation>(
                        stream, LoadingResult.CompiledResource, imageInformation));
                }
            }

            throw new FileNotFoundException(identifier);
        }
    }
}