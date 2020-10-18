using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class RoundedTransformation : ITransformation
    {
        public RoundedTransformation()
        {
			Helpers.ThrowOrDefault();
		}

        public RoundedTransformation(double radius)
        {
			Helpers.ThrowOrDefault();
		}

        public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio)
        {
			Helpers.ThrowOrDefault();
		}

        public RoundedTransformation(double radius, double cropWidthRatio, double cropHeightRatio, double borderSize, string borderHexColor)
        {
			Helpers.ThrowOrDefault();
		}

        public double Radius { get; set; }
        public double CropWidthRatio { get; set; }
        public double CropHeightRatio { get; set; }
        public double BorderSize { get; set; }
        public string BorderHexColor { get; set; }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            return Helpers.ThrowOrDefault<IBitmap>();
		}

		public string Key => Helpers.ThrowOrDefault<string>();
	}
}

