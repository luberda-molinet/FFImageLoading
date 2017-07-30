using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
	public class CircleTransformation : ITransformation
	{
		public CircleTransformation()
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

		public CircleTransformation(double borderSize, string borderHexColor)
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

		public double BorderSize { get; set; }
		public string BorderHexColor { get; set; }

		public string Key
		{
			get
			{
				throw new Exception(Common.DoNotReferenceMessage);
			}
		}
	}
}

