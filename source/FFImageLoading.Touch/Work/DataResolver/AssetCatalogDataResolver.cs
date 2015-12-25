using System;
using FFImageLoading.Work;
using System.IO;
using FFImageLoading.IO;
using System.Threading.Tasks;
using System.Threading;
using UIKit;
using Foundation;
using FFImageLoading.Helpers;

namespace FFImageLoading.Work.DataResolver
{
	public class AssetCatalogDataResolver : IDataResolver
	{
		private readonly IMainThreadDispatcher _mainThreadDispatcher;

		public AssetCatalogDataResolver(IMainThreadDispatcher mainThreadDispatcher)
		{
			_mainThreadDispatcher = mainThreadDispatcher;
		}

		public async Task<UIImageData> GetData(string identifier, CancellationToken token)
		{
			UIImage image = null;
			await _mainThreadDispatcher.PostAsync(() => image = UIImage.FromBundle(identifier)).ConfigureAwait(false);
			return new UIImageData() { Image = image, Result = LoadingResult.CompiledResource, ResultIdentifier = identifier };
		}

		public void Dispose() {
		}
		
	}
}

