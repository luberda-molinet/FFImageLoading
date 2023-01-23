using FFImageLoading.Work;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace FFImageLoading.DataResolvers
{
    public class FileDataResolver : IDataResolver
    {
        public async virtual Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            StorageFile file = null;

            try
            {
                var filePath = Path.GetDirectoryName(identifier);
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    file = await Cache.FFSourceBindingCache.GetFileAsync(identifier).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
            }

            if (file != null)
            {
                var imageInformation = new ImageInformation();
                imageInformation.SetPath(identifier);
                imageInformation.SetFilePath(identifier);

                token.ThrowIfCancellationRequested();
                var stream = await file.OpenStreamForReadAsync().ConfigureAwait(false);

                return new DataResolverResult(stream, LoadingResult.Disk, imageInformation);
            }

            throw new FileNotFoundException(identifier);
        }
    }
}
