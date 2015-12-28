using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
	public class CircleTransformation : ITransformation
	{
		private const string DoNotReference = "You are referencing the Portable version in your App - you need to reference the platform (iOS/Android) version";

		public CircleTransformation() : this(0d, null)
		{
			throw new Exception(DoNotReference);
		}

		public CircleTransformation(double borderSize, string borderHexColor)
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

