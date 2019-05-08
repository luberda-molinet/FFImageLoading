using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
	public class GrayscaleTransformation: ITransformation
	{
        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
		{
			return Helpers.ThrowOrDefault<IBitmap>();
		}

		public string Key => Helpers.ThrowOrDefault<string>();
	}
}

