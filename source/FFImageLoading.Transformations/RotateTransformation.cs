using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
    public class RotateTransformation : ITransformation
    {
        public RotateTransformation()
        {
			Helpers.ThrowOrDefault();
		}

        public RotateTransformation(double degrees) : this(degrees, false, false)
        {
			Helpers.ThrowOrDefault();
		}

        public RotateTransformation(double degrees, bool ccw) : this(degrees, ccw, false)
        {
			Helpers.ThrowOrDefault();
		}

        public RotateTransformation(double degrees, bool ccw, bool resize)
        {
			Helpers.ThrowOrDefault();
		}

        public double Degrees { get; set; }
        public bool CCW { get; set; }
        public bool Resize { get; set; }

		public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
			return Helpers.ThrowOrDefault<IBitmap>();
		}

		public string Key => Helpers.ThrowOrDefault<string>();
	}
}

