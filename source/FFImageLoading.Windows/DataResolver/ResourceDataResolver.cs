using FFImageLoading.Work;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace FFImageLoading.DataResolver
{
    class ResourceDataResolver : IDataResolver
    {
        private readonly ImageSource _source;

        public ResourceDataResolver(ImageSource source)
        {
            _source = source;
        }

        public async Task<ResolverImageData> GetData(string identifier, CancellationToken token)
        {
            byte[] bytes = null;

            StorageFile file = null;

            try
            {
                string resPath = @"Assets\" + identifier.TrimStart('\\', '/');
                file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(resPath);
            }
            catch (Exception)
            {
            }

            if (file != null)
            {
                bytes = await FilePathDataResolver.ReadFile(file);
            }

            return new ResolverImageData()
            {
                Result = LoadingResult.CompiledResource,
                ResultIdentifier = identifier,
                Data = bytes
            };
        }

        public void Dispose()
        {
        }
    }
}
