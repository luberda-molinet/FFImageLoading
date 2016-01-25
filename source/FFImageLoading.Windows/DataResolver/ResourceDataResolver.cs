using FFImageLoading.Work;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;

namespace FFImageLoading.DataResolver
{
    class ResourceDataResolver : IStreamResolver
    {
        private readonly ImageSource _source;

        public ResourceDataResolver(ImageSource source)
        {
            _source = source;
        }

        public async Task<WithLoadingResult<Stream>> GetStream(string identifier, CancellationToken token)
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
                return WithLoadingResult.Encapsulate(await file.OpenStreamForReadAsync(), LoadingResult.CompiledResource);

            return WithLoadingResult.Encapsulate<Stream>(null, LoadingResult.CompiledResource);
        }

        public void Dispose()
        {
        }
    }
}
