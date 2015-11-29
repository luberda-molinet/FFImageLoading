using FFImageLoading.Work;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace FFImageLoading.DataResolver
{
	public class FilePathDataResolver : IDataResolver
    {
        private readonly ImageSource _source;

        public FilePathDataResolver(ImageSource source)
        {
            _source = source;
        }

        public async Task<ResolverImageData> GetData(string identifier, CancellationToken token)
        {
            StorageFile file = null;

            try
            {
                file = await StorageFile.GetFileFromPathAsync(identifier);
            }
            catch (Exception)
            {
            }

            if (file != null)
            {
                var result = (LoadingResult)(int)_source;
                var bytes = await ReadFile(file);

                return new ResolverImageData() {
                    Data = bytes,
                    Result = result,
                    ResultIdentifier = identifier
                };
            }

            return null;
        }

        public static async Task<byte[]> ReadFile(StorageFile file)
        {
            byte[] fileBytes = null;
            using (IRandomAccessStreamWithContentType stream = await file.OpenReadAsync())
            {
                fileBytes = new byte[stream.Size];
                using (DataReader reader = new DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(fileBytes);
                }
            }

            return fileBytes;
        }

        public void Dispose()
        {
        }
    }
}
