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
			using (var asset = new NSDataAsset(identifier))
			{
				using (var stream = asset.Data.AsStream())
				{
					using (var memoryStream = new MemoryStream())
					{
						await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
						return new UIImageData() { Data = memoryStream.ToArray(), Result = LoadingResult.CompiledResource, ResultIdentifier = identifier };
					}
				}
			}
		}

		public void Dispose() {
		}
		
	}
}

