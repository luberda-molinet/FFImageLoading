using System;
using FFImageLoading.Work;
using System.IO;
using System.Threading.Tasks;
using Foundation;
using System.Linq;
using FFImageLoading.IO;
using System.Threading;

namespace FFImageLoading.DataResolvers
{
    public class BundleDataResolver : IDataResolver
	{
        readonly string[] fileTypes = { null, "png", "jpg", "jpeg", "webp", "PNG", "JPG", "JPEG", "WEBP"};

        public virtual Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            foreach (var fileType in fileTypes)
            {
                var bundle = NSBundle._AllBundles.FirstOrDefault(bu => !string.IsNullOrEmpty(bu.PathForResource(identifier, fileType)));
                if (bundle != null)
                {
                    var path = bundle.PathForResource(identifier, fileType);
                    var stream = FileStore.GetInputStream(path, true);

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