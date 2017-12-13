using System;
using Android.Graphics;

namespace FFImageLoading.Cache
{
    public interface IImageCache<TValue> : IMemoryCache<TValue>
    {
        /// <summary>
        /// Attempts to find a bitmap suitable for reuse based on the given dimensions.
        /// Note that any returned instance will have SetIsRetained(true) called on it
        /// to ensure that it does not release its resources prematurely as it is leaving
        /// cache management. This means you must call SetIsRetained(false) when you no
        /// longer need the instance.
        /// </summary>
        /// <returns>A SelfDisposingBitmapDrawable that has been retained. You must call SetIsRetained(false)
        /// when finished using it.</returns>
        /// <param name="options">Bitmap creation options.</param>
        TValue GetBitmapDrawableFromReusableSet(BitmapFactory.Options options);

        void AddToReusableSet(TValue value);
    }
}

