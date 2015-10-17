using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
	public class FlipTransformation : ITransformation
	{
		private const string DoNotReference = "You are referencing the Portable version in your App - you need to reference the platform (iOS/Android) version";

		public FlipTransformation(FlipType flipType)
		{
			throw new Exception(DoNotReference);
		}

		public IBitmap Transform(IBitmap source)
		{
			throw new Exception(DoNotReference);
		}
		public string Key
		{
			get
			{
				throw new Exception(DoNotReference);
			}
		}
	}
}

