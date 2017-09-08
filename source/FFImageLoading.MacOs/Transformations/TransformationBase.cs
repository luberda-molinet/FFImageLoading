using System;
using FFImageLoading.Work;
using AppKit;

namespace FFImageLoading.Transformations
{
    public abstract class TransformationBase : ITransformation
	{
        public abstract string Key { get; }

        public IBitmap Transform(IBitmap bitmapHolder, string path, ImageSource source, bool isPlaceholder, string key)
        {
            var sourceBitmap = bitmapHolder.ToNative();
            return new BitmapHolder(Transform(sourceBitmap) ?? Transform(sourceBitmap, path, source, isPlaceholder, key));
        }

        [Obsolete("Use the new override")]
        protected virtual NSImage Transform(NSImage sourceBitmap)
        {
            return null;
        }

        protected virtual NSImage Transform(NSImage sourceBitmap, string path, ImageSource source, bool isPlaceholder, string key)
        {
            return sourceBitmap;
        }
	}
}

