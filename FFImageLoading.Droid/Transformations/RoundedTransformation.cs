using System;
using FFImageLoading.Work;
using Android.Graphics;

namespace FFImageLoading.Transformations
{
	public class RoundedTransformation : TransformationBase, IMultiplatformTransformation
	{
		double radius;

		public RoundedTransformation(double radius)
		{
			this.radius = radius;
		}

		public void SetParameters(object[] parameters)
		{
			this.radius = (double)parameters[0];
		}

		public override string Key
		{
			get { return string.Format("RoundedTransformation, radius = {0}", radius); }
		}
			
		protected override Bitmap Transform(Bitmap source)
		{
			var transformed = ToRounded(source, (float)radius);
			source.Recycle();

			return transformed;
		}

		public static Bitmap ToRounded(Bitmap source, float rad)
		{
			int size = Math.Min(source.Width, source.Height);

			int width = (source.Width - size) / 2;
			int height = (source.Height - size) / 2;

			Bitmap bitmap = Bitmap.CreateBitmap(size, size, Bitmap.Config.Argb8888);

			using (Canvas canvas = new Canvas(bitmap))
			using (Paint paint = new Paint())
			using (BitmapShader shader = new BitmapShader(source, Shader.TileMode.Clamp, Shader.TileMode.Clamp))
			using (Matrix matrix = new Matrix())
			{
				if (width != 0 || height != 0)
				{
					// source isn't square, move viewport to centre
					matrix.SetTranslate(-width, -height);
					shader.SetLocalMatrix(matrix);
				}
				paint.SetShader(shader);
				paint.AntiAlias = true;

				canvas.DrawCircle(rad, rad, rad, paint);

				return bitmap;				
			}
		}
	}
}

