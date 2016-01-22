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
                string resPath = @"Assets\" + identifier.TrimStart('\\', '/');
                var imgUri = new Uri("ms-appx:///" + resPath);
                file = await StorageFile.GetFileFromApplicationUriAsync(imgUri);
                // OLD WAY: file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(resPath);
            }
            catch (Exception)
            {
            }

            return WithLoadingResult.Encapsulate(await file.OpenStreamForReadAsync(), LoadingResult.CompiledResource);
        }

        public void Dispose()
        {
        }
    }
}
