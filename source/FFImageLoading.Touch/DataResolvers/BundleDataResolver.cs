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
        public Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            var fileName = Path.GetFileNameWithoutExtension(identifier);
            var extension = Path.GetExtension(identifier).TrimStart('.');

            var bundle = NSBundle._AllBundles.FirstOrDefault(bu => !string.IsNullOrEmpty(bu.PathForResource(fileName, extension)));

            if (bundle != null)
            {
                var url = bundle.GetUrlForResource(fileName, extension);
                var stream = FileStore.GetInputStream(url.Path, true);

                var imageInformation = new ImageInformation();
                imageInformation.SetPath(identifier);
                imageInformation.SetFilePath(url.Path);

                return Task.FromResult(new Tuple<Stream, LoadingResult, ImageInformation>(
                    stream, LoadingResult.CompiledResource, imageInformation));
            }

            throw new FileNotFoundException(identifier);
        }
    }
}