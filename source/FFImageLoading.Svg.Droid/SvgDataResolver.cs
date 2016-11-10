using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;

namespace FFImageLoading.Svg.Platform
{
	public class SvgDataResolver : IVectorDataResolver
	{
		public SvgDataResolver(int vectorWidth, int vectorHeight, bool useDipUnits)
		{
			VectorWidth = vectorWidth;
			VectorHeight = vectorHeight;
			UseDipUnits = useDipUnits;
		}

		public bool UseDipUnits { get; private set; }

		public int VectorHeight { get; private set; }

		public int VectorWidth { get; private set; }

		public Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
		{
			throw new NotImplementedException();
		}
	}
}
