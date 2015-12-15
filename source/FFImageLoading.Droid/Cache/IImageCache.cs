using System;
using Android.Graphics.Drawables;
using Android.Graphics;

namespace FFImageLoading.Cache
{
	public interface IImageCache : IMemoryCache<BitmapDrawable>
    {
        Bitmap GetBitmapFromReusableSet(BitmapFactory.Options options);
    }
}

