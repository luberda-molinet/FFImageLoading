using System;
using Android.Graphics;

namespace FFImageLoading.Transformations
{
	public class TintTransformation : ColorSpaceTransformation
	{
		public TintTransformation() : this(0, 165, 0, 128)
		{
		}

		public TintTransformation(int r, int g, int b, int a)
		{
			R = r;
			G = g;
			B = b;
			A = a;
		}

		public TintTransformation(string hexColor)
		{
			HexColor = hexColor;
		}

		string _hexColor;
		public string HexColor
		{
			get
			{
				return _hexColor;
			}

			set
			{
				_hexColor = value;
				var color = value.ToColor();
				R = color.R;
				G = color.G;
				B = color.B;
				A = color.A;
			}
		}

		public int R { get; set; }

		public int G { get; set; }

		public int B { get; set; }

		public int A { get; set; }

		public override string Key
		{
			get 
			{ 
				return string.Format("TintTransformation,R={0},G={1},B={2},A={3},HexColor={4}", 
				                     R, G, B, A, HexColor); 
			}
		}

		protected override Bitmap Transform(Bitmap source)
		{
			RGBAWMatrix = FFColorMatrix.ColorToTintMatrix(R, G, B, A);

			return base.Transform(source);
		}
	}
}

