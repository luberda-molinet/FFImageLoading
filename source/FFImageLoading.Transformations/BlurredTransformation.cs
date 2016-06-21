using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
	public class BlurredTransformation: ITransformation
	{
		public BlurredTransformation()
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

		public BlurredTransformation(double radius)
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

		public double Radius { get; set; }

		public IBitmap Transform(IBitmap source)
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

		public string Key
		{
			get
			{
				throw new Exception(Common.DoNotReferenceMessage);
			}
		}
	}
}

