using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;
using FFImageLoading.IO;
using AppFW = Tizen.Applications;

namespace FFImageLoading.DataResolvers
{
    public class FileDataResolver : IDataResolver
    {
        public virtual Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            var filePath = GetPath(identifier);
            if (!FileStore.Exists(filePath))
            {
                throw new FileNotFoundException(identifier);
            }

            token.ThrowIfCancellationRequested();

            var stream = FileStore.GetInputStream(filePath, true);

            var imageInformation = new ImageInformation();
            imageInformation.SetPath(identifier);
            imageInformation.SetFilePath(filePath);

            return Task.FromResult(new DataResolverResult(
                stream, LoadingResult.Disk, imageInformation));
        }

        static string GetPath(string res)
        {
            if (Path.IsPathRooted(res))
            {
                return res;
            }

            foreach (AppFW.ResourceManager.Category category in Enum.GetValues(typeof(AppFW.ResourceManager.Category)))
            {
                var path = AppFW.ResourceManager.TryGetPath(category, res);

                if (path != null)
                {
                    return path;
                }
            }

            AppFW.Application app = AppFW.Application.Current;
            if (app != null)
            {
                string resPath = app.DirectoryInfo.Resource + res;
                if (File.Exists(resPath))
                {
                    return resPath;
                }
            }
            return res;
        }
    }
}

