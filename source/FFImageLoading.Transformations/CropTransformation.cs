using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class CropTransformation : ITransformation
    {
        public CropTransformation()
        {
			Helpers.ThrowOrDefault();
		}

        public CropTransformation(double zoomFactor, double xOffset, double yOffset)
        {
			Helpers.ThrowOrDefault();
		}

        public CropTransformation(double zoomFactor, double xOffset, double yOffset, double cropWidthRatio, double cropHeightRatio)
        {
			Helpers.ThrowOrDefault();
		}

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
			return Helpers.ThrowOrDefault<IBitmap>();
		}

        public double ZoomFactor { get; set; }
        public double XOffset { get; set; }
        public double YOffset { get; set; }
        public double CropWidthRatio { get; set; }
        public double CropHeightRatio { get; set; }

		public string Key => Helpers.ThrowOrDefault<string>();
	}
}

