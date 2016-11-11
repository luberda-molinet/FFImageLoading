using System;
using System.IO;
using FFImageLoading.Work;
using Android.Content;
using Android.Content.Res;
using System.Threading.Tasks;
using System.Threading;

namespace FFImageLoading.DataResolvers
{
    public class BundleDataResolver : IDataResolver
    {
        public virtual Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var stream = Context.Assets.Open(identifier, Access.Streaming);

            if (stream == null)
                throw new FileNotFoundException(identifier);

            var imageInformation = new ImageInformation();
            imageInformation.SetPath(identifier);
            imageInformation.SetFilePath(null);

            return Task.FromResult(new Tuple<Stream, LoadingResult, ImageInformation>(
                stream, LoadingResult.ApplicationBundle, imageInformation));
        }

        protected Context Context { get { return new ContextWrapper(Android.App.Application.Context); } }
    }
}

