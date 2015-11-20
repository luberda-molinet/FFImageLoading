using System;
using Android.Graphics.Drawables;
using Android.Graphics;

namespace FFImageLoading.Cache
{
    public interface IImageCache
    {
        BitmapDrawable Get(string key);
        Bitmap GetBitmapFromReusableSet(BitmapFactory.Options options);
        void Add(string key, BitmapDrawable bitmap);
		void Clear();
		void Remove(string key);
    }
}

