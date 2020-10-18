using System;
using FFImageLoading.Work;
using System.IO;
using FFImageLoading.IO;
using System.Threading.Tasks;
using FFImageLoading.Helpers;
using System.Threading;

namespace FFImageLoading.DataResolvers
{
    public class FileDataResolver : IDataResolver
    {
        public virtual Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            string file = null;

            var scale = (int)ScaleHelper.Scale;
            if (scale > 1)
            {
                var filename = Path.GetFileNameWithoutExtension(identifier);
                var extension = Path.GetExtension(identifier);
                const string pattern = "{0}@{1}x{2}";

                while (scale > 1)
                {
                    token.ThrowIfCancellationRequested();

                    var tmpFile = string.Format(pattern, filename, scale, extension);
                    if (FileStore.Exists(tmpFile))
                    {
                        file = tmpFile;
                        break;
                    }
                    scale--;
                }
            }

            if (string.IsNullOrEmpty(file) && FileStore.Exists(identifier))
            {
                file = identifier;
            }

            token.ThrowIfCancellationRequested();

            if (!string.IsNullOrEmpty(file))
            {
                var stream = FileStore.GetInputStream(file, true);
                var imageInformation = new ImageInformation();
                imageInformation.SetPath(identifier);
                imageInformation.SetFilePath(file);

                var result = (LoadingResult)(int)parameters.Source;

                if (parameters.LoadingPlaceholderPath == identifier)
                    result = (LoadingResult)(int)parameters.LoadingPlaceholderSource;
                else if (parameters.ErrorPlaceholderPath == identifier)
                    result = (LoadingResult)(int)parameters.ErrorPlaceholderSource;

                return Task.FromResult(new DataResolverResult(
                    stream, result, imageInformation));
            }

            throw new FileNotFoundException(identifier);
        }
    }
}

