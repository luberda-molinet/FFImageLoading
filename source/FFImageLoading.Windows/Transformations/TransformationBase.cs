using System;
using FFImageLoading.Work;

namespace FFImageLoading.Transformations
{
    public abstract class TransformationBase : ITransformation
    {
        public abstract string Key { get; }

        public IBitmap Transform(IBitmap bitmapHolder, string path, ImageSource source, bool isPlaceholder, string key)
        {
            var transformed = Transform(bitmapHolder) ?? Transform(bitmapHolder, path, source, isPlaceholder, key);
            return transformed.ToNative();
        }

        [Obsolete("Use the new override")]
        protected virtual BitmapHolder Transform(BitmapHolder bitmapHolder)
        {
        	return null;
        }

        protected virtual BitmapHolder Transform(BitmapHolder bitmapHolder, string path, ImageSource source, bool isPlaceholder, string key)
        {
        	return bitmapHolder;
        }
    }
}
