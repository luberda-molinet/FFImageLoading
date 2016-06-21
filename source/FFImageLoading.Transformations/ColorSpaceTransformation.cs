using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
	public class ColorSpaceTransformation: ITransformation
	{
		public ColorSpaceTransformation()
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

		public ColorSpaceTransformation(float[][] rgbawMatrix)
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

		public IBitmap Transform(IBitmap source)
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

		public float[][] RGBAWMatrix { get; set; }

		public string Key
		{
			get
			{
				throw new Exception(Common.DoNotReferenceMessage);
			}
		}
	}
}

