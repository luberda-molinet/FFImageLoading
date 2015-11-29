using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
	public class ColorSpaceTransformation: ITransformation
	{
		private const string DoNotReference = "You are referencing the Portable version in your App - you need to reference the platform (iOS/Android) version";

		public ColorSpaceTransformation(float[][] rgbawMatrix)
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

