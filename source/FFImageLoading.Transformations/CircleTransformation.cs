using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
	public class CircleTransformation : ITransformation
	{
		public CircleTransformation()
		{
			Helpers.ThrowOrDefault();
		}

		public CircleTransformation(double borderSize, string borderHexColor)
		{
			Helpers.ThrowOrDefault();
		}

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
		{
			return Helpers.ThrowOrDefault<IBitmap>();
		}

		public double BorderSize { get; set; }
		public string BorderHexColor { get; set; }

		public string Key => Helpers.ThrowOrDefault<string>();
	}
}

