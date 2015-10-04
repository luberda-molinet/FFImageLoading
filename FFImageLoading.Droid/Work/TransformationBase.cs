using System;
using Android.Graphics;

namespace FFImageLoading.Work
{
	public abstract class TransformationBase: ITransformation
	{
		public abstract string Key { get; }

		public IBitmap Transform(IBitmap source)
		{
			return new BitmapHolder(Transform(source.ToNative()));
		}

		public abstract void SetParameters(object[] parameters);

		protected abstract Bitmap Transform(Bitmap source);
	}
}

