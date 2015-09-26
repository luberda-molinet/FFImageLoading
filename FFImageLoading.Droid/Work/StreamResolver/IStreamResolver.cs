using System;
using FFImageLoading.Work;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace FFImageLoading.Work.StreamResolver
{
	public interface IStreamResolver : IDisposable
	{

		Task<WithLoadingResult<Stream>> GetStream(string identifier, CancellationToken token);

	}
}

