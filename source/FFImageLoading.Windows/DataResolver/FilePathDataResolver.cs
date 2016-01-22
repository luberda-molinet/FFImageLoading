using FFImageLoading.Work;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace FFImageLoading.DataResolver
{
	public class FilePathDataResolver :  IStreamResolver
    {
        private readonly ImageSource _source;

        public FilePathDataResolver(ImageSource source)
        {
            _source = source;
        }

        public async Task<WithLoadingResult<Stream>> GetStream(string identifier, CancellationToken token)
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

            return WithLoadingResult.Encapsulate(await file.OpenStreamForReadAsync(), LoadingResult.Disk);
        }

        public void Dispose()
        {
        }
    }
}
