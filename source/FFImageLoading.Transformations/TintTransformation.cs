using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    [Preserve(AllMembers = true)]
	public class TintTransformation : ITransformation
	{
		public TintTransformation()
		{
			Helpers.ThrowOrDefault();
		}

		public TintTransformation(int r, int g, int b, int a)
		{
			Helpers.ThrowOrDefault();
		}

		public TintTransformation(string hexColor)
		{
			Helpers.ThrowOrDefault();
		}

		public bool EnableSolidColor { get; set; }

		public string HexColor { get; set; }

		public int R { get; set; }

		public int G { get; set; }

		public int B { get; set; }

		public int A { get; set; }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
		{
			return Helpers.ThrowOrDefault<IBitmap>();
		}

		public string Key => Helpers.ThrowOrDefault<string>();
	}
}

