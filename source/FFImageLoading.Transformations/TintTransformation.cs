using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
	public class TintTransformation : ITransformation
	{
		public TintTransformation()
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

		public TintTransformation(int r, int g, int b, int a)
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

		public TintTransformation(string hexColor)
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

		public bool EnableSolidColor { get; set; }

		public string HexColor { get; set; }

		public int R { get; set; }

		public int G { get; set; }

		public int B { get; set; }

		public int A { get; set; }

        public IBitmap Transform(IBitmap sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
		{
			throw new Exception(Common.DoNotReferenceMessage);
		}

		public string Key
		{
			get
			{
				throw new Exception(Common.DoNotReferenceMessage);
			}
		}
	}
}

