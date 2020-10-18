using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public class ColorFillTransformation : ITransformation
    {
        public ColorFillTransformation()
        {
			Helpers.ThrowOrDefault();
		}

        public ColorFillTransformation(string hexColor)
        {
			Helpers.ThrowOrDefault();
		}

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
			return Helpers.ThrowOrDefault<IBitmap>();
		}

        public string HexColor { get; set; }

		public string Key => Helpers.ThrowOrDefault<string>();
	}
}
