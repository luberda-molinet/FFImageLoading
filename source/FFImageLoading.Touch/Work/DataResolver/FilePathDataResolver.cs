using System;
using FFImageLoading.Work;
using System.IO;
using FFImageLoading.IO;
using System.Threading.Tasks;
using System.Threading;
using UIKit;
using CoreFoundation;
using FFImageLoading.Helpers;

namespace FFImageLoading.Work.DataResolver
{
	public class FilePathDataResolver : IDataResolver
	{
		private readonly ImageSource _source;

		public FilePathDataResolver(ImageSource source)
		{
			_source = source;
		}

		public Task<UIImageData> GetData(string identifier, CancellationToken token)
		{
			int scale = (int)ScaleHelper.Scale;
			if (scale > 1)
			{
				var filename = Path.GetFileNameWithoutExtension(identifier);
				var extension = Path.GetExtension(identifier);
				const string pattern = "{0}@{1}x{2}";

				while (scale > 1)
				{
					var file = String.Format(pattern, filename, scale, extension);
					if (FileStore.Exists(file))
					{
						return GetDataInternal(file, token);
					}
					scale--;
				}
			}

			if (FileStore.Exists(identifier))
			{
				return GetDataInternal(identifier, token);
			}

			return Task.FromResult((UIImageData)null);
		}

		public void Dispose() {
		}

		private async Task<UIImageData> GetDataInternal(string identifier, CancellationToken token)
		{
			var bytes = await FileStore.ReadBytesAsync(identifier, token).ConfigureAwait(false);
			var result = (LoadingResult)(int)_source; // Some values of ImageSource and LoadingResult are shared
			return new UIImageData() { Data = bytes, Result = result, ResultIdentifier = identifier };
		}
		
	}
}

