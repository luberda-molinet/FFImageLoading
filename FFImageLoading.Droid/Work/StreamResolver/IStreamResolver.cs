using System;
using FFImageLoading.Work;
using System.Threading.Tasks;
using System.IO;

namespace FFImageLoading
{
	public interface IStreamResolver : IDisposable
	{

		Task<WithLoadingResult<Stream>> GetStream(string identifier);

	}
}

