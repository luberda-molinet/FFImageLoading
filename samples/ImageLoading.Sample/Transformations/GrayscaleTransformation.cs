using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Work;

namespace ImageLoading.Sample.Transformations
{
    /// <summary>
    /// https://github.com/wasabeef/picasso-transformations
    /// </summary>
	public class GrayscaleTransformation : TransformationBase
    {
        public override string Key
        {
            get { return "GrayscaleTransformation()"; }
        }

        protected override Bitmap Transform(Bitmap source)
        {
            int width = source.Width;
            int height = source.Height;

            Bitmap bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);

            Canvas canvas = new Canvas(bitmap);
            ColorMatrix saturation = new ColorMatrix();
            saturation.SetSaturation(0f);
            Paint paint = new Paint();
            paint.SetColorFilter(new ColorMatrixColorFilter(saturation));
            canvas.DrawBitmap(source, 0, 0, paint);
            source.Recycle();

            return bitmap;
        }
    }
}