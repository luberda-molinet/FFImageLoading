using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public abstract class TransformationBase : ITransformation
    {
        public abstract string Key { get; }

        public IBitmap Transform(IBitmap bitmapHolder, string path, ImageSource source, bool isPlaceholder, string key)
        {
            var nativeHolder = bitmapHolder.ToNative();
            return Transform(nativeHolder, path, source, isPlaceholder, key);
        }

        protected virtual BitmapHolder Transform(BitmapHolder bitmapHolder, string path, ImageSource source, bool isPlaceholder, string key)
        {
            return bitmapHolder;
        }
    }
}
