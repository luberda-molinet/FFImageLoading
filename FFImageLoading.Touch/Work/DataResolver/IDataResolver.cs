using System;
using FFImageLoading.Work;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace FFImageLoading.Work.DataResolver
{
	public interface IDataResolver : IDisposable
	{

		Task<UIImageData> GetData(string identifier, CancellationToken token);

	}
}

