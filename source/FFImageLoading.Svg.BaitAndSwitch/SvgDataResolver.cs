using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;

namespace FFImageLoading.Svg.Platform
{
	public class SvgDataResolver : IVectorDataResolver
	{
		const string DoNotReference = "You are referencing the Portable version in your App - you need to reference the platform version";

		public SvgDataResolver(int vectorWidth, int vectorHeight, bool useDipUnits)
		{
			throw new Exception(DoNotReference);
		}

		public bool UseDipUnits
		{
			get
			{
				throw new Exception(DoNotReference);
			}
		}

		public int VectorHeight
		{
			get
			{
				throw new Exception(DoNotReference);
			}
		}

		public int VectorWidth
		{
			get
			{
				throw new Exception(DoNotReference);
			}
		}

		public Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
		{
			throw new Exception(DoNotReference);
		}
	}
}
