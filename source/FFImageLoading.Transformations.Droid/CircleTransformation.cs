using System;
using FFImageLoading.Work;
using Android.Graphics;
using Android.Runtime;

namespace FFImageLoading.Transformations
{
	[Preserve(AllMembers = true)]
	public class CircleTransformation : TransformationBase
	{
		private double _borderSize;
		private string _borderHexColor;

		public CircleTransformation() : this(0d, null)
		{
		}

		public CircleTransformation(double borderSize, string borderHexColor)
		{
			_borderSize = borderSize;
			_borderHexColor = borderHexColor;
		}

		public override string Key
		{
			get { return string.Format("CircleTransformation,borderSize={0},borderHexColor={1}", _borderSize, _borderHexColor); }
		}

		protected override Bitmap Transform(Bitmap source)
		{
			return RoundedTransformation.ToRounded(source, 0f, 1f, 1f, _borderSize, _borderHexColor);
		}
	}
}

