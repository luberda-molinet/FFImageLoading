using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
	public class RoundedTransformation : ITransformation
	{
		private const string DoNotReference = "You are referencing the Portable version in your App - you need to reference the platform (iOS/Android) version";

		public RoundedTransformation(double radius)
		{
			throw new Exception(DoNotReference);
		}

		public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio)
		{
			throw new Exception(DoNotReference);
		}

		public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio, double borderSize, string borderHexColor)
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

