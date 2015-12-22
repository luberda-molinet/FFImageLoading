using System;
using FFImageLoading.Work;
using System.IO;
using FFImageLoading.IO;
using System.Threading.Tasks;
using System.Threading;
using UIKit;

namespace FFImageLoading.Work.DataResolver
{
	public class AssetCatalogDataResolver : IDataResolver
	{
		public async Task<UIImageData> GetData(string identifier, CancellationToken token)
		{
			return await Task.Run(() =>
				{
					var image = UIImage.FromBundle(identifier);
					return new UIImageData() { Image = image, Result = LoadingResult.CompiledResource, ResultIdentifier = identifier };
				}).ConfigureAwait(false);
		}

		public void Dispose() {
		}
		
	}
}

