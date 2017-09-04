using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
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

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
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

