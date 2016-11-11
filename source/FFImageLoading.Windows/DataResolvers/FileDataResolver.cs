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
        public async virtual Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            StorageFile file = null;

            try
            {
                var filePath = Path.GetDirectoryName(identifier);

                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    file = await StorageFile.GetFileFromPathAsync(identifier);
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
                var stream = await file.OpenStreamForReadAsync();

                return new Tuple<Stream, LoadingResult, ImageInformation>(stream, LoadingResult.Disk, imageInformation);
            }

            throw new FileNotFoundException(identifier);
        }
    }
}
