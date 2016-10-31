using FFImageLoading.Work;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;

namespace FFImageLoading.DataResolvers
{
    public class ResourceDataResolver : IDataResolver
    {
        public async virtual Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            StorageFile file = null;

            try
            {
                string resPath = identifier.TrimStart('\\', '/');

                if (!resPath.StartsWith(@"Assets\") && !resPath.StartsWith("Assets/"))
                {
                    resPath = @"Assets\" + resPath;
                }

                var imgUri = new Uri("ms-appx:///" + resPath);
                file = await StorageFile.GetFileFromApplicationUriAsync(imgUri);
            }
            catch (Exception)
            {
                try
                {
                    var imgUri = new Uri("ms-appx:///" + identifier);
                    file = await StorageFile.GetFileFromApplicationUriAsync(imgUri);
                }
                catch (Exception)
                {
                }
            }


            if (file != null)
            {
                var imageInformation = new ImageInformation();
                imageInformation.SetPath(identifier);
                imageInformation.SetFilePath(file.Path);

                var stream = await file.OpenStreamForReadAsync();

                return new Tuple<Stream, LoadingResult, ImageInformation>(stream, LoadingResult.CompiledResource, imageInformation);
            }

            throw new FileNotFoundException(identifier);
        }
    }
}
