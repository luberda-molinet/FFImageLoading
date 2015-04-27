using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using FFImageLoading;
using FFImageLoading.Work;

namespace ImageLoading.Sample.Transformations
{
    /// <summary>
    /// https://github.com/wasabeef/picasso-transformations
    /// </summary>
	public class RoundedCornersTransformation : TransformationBase, ITransformation
    {
        private int radius;
        private int margin;

        public RoundedCornersTransformation(int radius, int margin)
        {
            this.radius = radius;
            this.margin = margin;
        }

        public override string Key
        {
            get { return "RoundedTransformation(radius=" + radius + ", margin=" + margin + ")"; }
        }

        protected override Bitmap Transform(Bitmap source)
        {
            int width = source.Width;
            int height = source.Height;

            Bitmap bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);

            Canvas canvas = new Canvas(bitmap);
            Paint paint = new Paint();
            paint.AntiAlias = true;
            paint.SetShader(new BitmapShader(source, Shader.TileMode.Clamp, Shader.TileMode.Clamp));
            canvas.DrawRoundRect(new RectF(margin, margin, width - margin, height - margin), radius, radius, paint);
            source.Recycle();

            return bitmap;
        }
    }
}