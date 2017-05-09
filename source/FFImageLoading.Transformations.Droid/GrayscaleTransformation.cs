using Android.Graphics;
using Android.Runtime;

namespace FFImageLoading.Transformations
{
	[Preserve(AllMembers = true)]
	public class GrayscaleTransformation : TransformationBase
	{
		public GrayscaleTransformation()
		{
		}

		public override string Key
		{
			get { return "GrayscaleTransformation"; }
		}

		protected override Bitmap Transform(Bitmap sourceBitmap, string path, Work.ImageSource source, bool isPlaceholder, string key)
		{
			return ToGrayscale(sourceBitmap);
		}

		public static Bitmap ToGrayscale(Bitmap source)
		{
			using (var colorMatrix = new ColorMatrix())
			{
				colorMatrix.SetSaturation(0f);
				return ColorSpaceTransformation.ToColorSpace(source, colorMatrix);
			}
		}
	}
}

