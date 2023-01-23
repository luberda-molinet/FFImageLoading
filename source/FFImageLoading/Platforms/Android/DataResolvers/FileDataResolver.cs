using FFImageLoading.Work;
using System.IO;
using FFImageLoading.IO;
using System.Threading.Tasks;
using System.Threading;

namespace FFImageLoading.DataResolvers
{
    public class FileDataResolver : IDataResolver
    {
        public virtual Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            if (!FileStore.Exists(identifier))
            {
                throw new FileNotFoundException(identifier);
            }

            token.ThrowIfCancellationRequested();

            var stream = FileStore.GetInputStream(identifier, true);

            var imageInformation = new ImageInformation();
            imageInformation.SetPath(identifier);
            imageInformation.SetFilePath(identifier);

            return Task.FromResult(new DataResolverResult(
                stream, LoadingResult.Disk, imageInformation));
        }
    }
}

