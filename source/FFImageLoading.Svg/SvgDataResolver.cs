using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFImageLoading.Work;

namespace FFImageLoading.Svg.Platform
{
	public class SvgDataResolver : IVectorDataResolver
	{
		public static string DoNotReferenceMessage
		{
			get
			{
				return "You are referencing the Portable version in your App - you need to reference the platform specific version";
			}
		}

		public bool UseDipUnits
		{
			get
			{
				throw new Exception(DoNotReferenceMessage);
			}
		}

		public int VectorHeight
		{
			get
			{
				throw new Exception(DoNotReferenceMessage);
			}
		}

		public int VectorWidth
		{
			get
			{
				throw new Exception(DoNotReferenceMessage);
			}
		}

		public Task<Tuple<Stream, LoadingResult, ImageInformation>> Resolve(string identifier, TaskParameter parameters, CancellationToken token)
		{
			throw new Exception(DoNotReferenceMessage);
		}
	}
}
