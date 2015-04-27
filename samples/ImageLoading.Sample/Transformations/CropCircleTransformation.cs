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
	public class CropCircleTransformation : TransformationBase, ITransformation
    {
        public override string Key
        {
            get { return "CropCircleTransformation()"; }
        }

        protected override Bitmap Transform(Bitmap source)
        {
            int size = Math.Min(source.Width, source.Height);

            int width = (source.Width - size) / 2;
            int height = (source.Height - size) / 2;

            Bitmap bitmap = Bitmap.CreateBitmap(size, size, Bitmap.Config.Argb8888);

            Canvas canvas = new Canvas(bitmap);
            Paint paint = new Paint();
            BitmapShader shader = new BitmapShader(source, Shader.TileMode.Clamp, Shader.TileMode.Clamp);

            if (width != 0 || height != 0)
            {
                // source isn't square, move viewport to centre
                Matrix matrix = new Matrix();
                matrix.SetTranslate(-width, -height);
                shader.SetLocalMatrix(matrix);
            }
            paint.SetShader(shader);
            paint.AntiAlias = true;

            float r = size / 2f;

            canvas.DrawCircle(r, r, r, paint);

            source.Recycle();

            return bitmap;
        }
    }
}