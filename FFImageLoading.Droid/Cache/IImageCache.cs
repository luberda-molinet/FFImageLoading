using System;
using Android.Graphics.Drawables;
using Android.Graphics;

namespace FFImageLoading.Cache
{
    public interface IImageCache : ILruCache<BitmapDrawable>
    {
        int Count { get; set; }
        void Add(string key, BitmapDrawable bitmap);
        Bitmap GetBitmapFromReusableSet(BitmapFactory.Options options);
    }
}

