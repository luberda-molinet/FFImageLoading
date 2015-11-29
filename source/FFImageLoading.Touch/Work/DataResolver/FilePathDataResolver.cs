using System;
using FFImageLoading.Work;
using System.IO;
using FFImageLoading.IO;
using System.Threading.Tasks;
using System.Threading;
using UIKit;

namespace FFImageLoading.Work.DataResolver
{
	public class FilePathDataResolver : IDataResolver
	{
		private readonly ImageSource _source;

		public FilePathDataResolver(ImageSource source)
		{
			_source = source;
		}

		public async Task<UIImageData> GetData(string identifier, CancellationToken token)
		{
			if (!FileStore.Exists(identifier))
				return null;

			var bytes = await FileStore.ReadBytesAsync(identifier).ConfigureAwait(false);
			var result = (LoadingResult)(int)_source; // Some values of ImageSource and LoadingResult are shared
			return new UIImageData() { Data = bytes, Result = result, ResultIdentifier = identifier };
		}

		public void Dispose() {
		}
		
	}
}

