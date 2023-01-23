using System;
using Android.Graphics;

namespace FFImageLoading.Drawables
{
	public interface ISelfDisposingAnimatedBitmapDrawable : ISelfDisposingBitmapDrawable
	{
		IAnimatedImage<Bitmap>[] AnimatedImages { get; }
	}
}
