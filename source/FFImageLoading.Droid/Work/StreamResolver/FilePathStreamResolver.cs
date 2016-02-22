using System;
using Android.Graphics.Drawables;
using FFImageLoading.Work;
using System.IO;
using FFImageLoading.IO;
using System.Threading.Tasks;
using System.Threading;

namespace FFImageLoading.Work.StreamResolver
{
	public class FilePathStreamResolver : IStreamResolver
	{
		
		public Task<WithLoadingResult<Stream>> GetStream(string identifier, CancellationToken token)
		{
			if (!FileStore.Exists(identifier))
			{
				return Task.FromResult(WithLoadingResult.Encapsulate((Stream)null, LoadingResult.NotFound));
			}

			var imageInformation = new ImageInformation();
			imageInformation.SetPath(identifier);
			imageInformation.SetFilePath(identifier);

			var result = WithLoadingResult.Encapsulate(FileStore.GetInputStream(identifier), LoadingResult.Disk, imageInformation);
			return Task.FromResult(result);
		}

		public void Dispose() {
		}
		
	}
}

