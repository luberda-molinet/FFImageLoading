using System;
using Android.Graphics.Drawables;
using FFImageLoading.Work;
using System.IO;
using FFImageLoading.IO;
using System.Threading.Tasks;

namespace FFImageLoading
{
	public class FilePathStreamResolver : IStreamResolver
	{
		
		public async Task<WithLoadingResult<Stream>> GetStream(string identifier)
		{
			return WithLoadingResult.Encapsulate(FileStore.GetInputStream(identifier), LoadingResult.Disk);
		}

		public void Dispose() {
		}
		
	}
}

