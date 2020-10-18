using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers=true)]
	public class BlurredTransformation : ITransformation
	{
		public BlurredTransformation()
		{
			Helpers.ThrowOrDefault();
		}

		public BlurredTransformation(double radius)
		{
			Helpers.ThrowOrDefault();
		}

		public double Radius { get; set; }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
		{
			return Helpers.ThrowOrDefault<IBitmap>();
		}

		public string Key => Helpers.ThrowOrDefault<string>();
	}
}

