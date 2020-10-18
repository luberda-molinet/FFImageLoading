using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
	public class FlipTransformation : ITransformation
	{
		public FlipTransformation()
		{
			Helpers.ThrowOrDefault();
		}

		public FlipTransformation(FlipType flipType)
		{
			Helpers.ThrowOrDefault();
		}

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
		{
			return Helpers.ThrowOrDefault<IBitmap>();
		}

        public FlipType FlipType { get; set; }

		public string Key => Helpers.ThrowOrDefault<string>();
	}
}

