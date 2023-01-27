using FFImageLoading.Work;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.Resources.Core;

namespace FFImageLoading.DataResolvers
{
    public class ResourceDataResolver : IDataResolver
    {
        public async virtual Task<DataResolverResult> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

			var resourceContext = new ResourceContext(); // not using ResourceContext.GetForCurrentView

			var scale = ((int)(parameters.Scale * 100)).ToString();
			resourceContext.QualifierValues["scale"] = scale;

			var resMgr = ResourceManager.Current;

			if (resMgr.MainResourceMap.TryGetValue($"Files/{identifier}", out var namedResource))
			{
				if (namedResource != null)
				{
					var resourceCandidate = namedResource.Resolve(resourceContext);

					if (resourceCandidate != null)
					{
						var imageInformation = new ImageInformation();
						imageInformation.SetPath(identifier);
						imageInformation.SetFilePath(resourceCandidate.ValueAsString);

						token.ThrowIfCancellationRequested();

						var stream = await resourceCandidate.GetValueAsStreamAsync();

						return new DataResolverResult(stream.AsStream(), LoadingResult.CompiledResource, imageInformation);
					}
				}
			}

            throw new FileNotFoundException(identifier);
        }
    }
}
