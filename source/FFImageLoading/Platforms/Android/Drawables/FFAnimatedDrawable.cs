using Android.Content.Res;
using Android.Graphics;
using System;
using Android.Runtime;
using System.IO;

namespace FFImageLoading.Drawables
{
    public class FFAnimatedDrawable : SelfDisposingBitmapDrawable, ISelfDisposingAnimatedBitmapDrawable
	{
        public FFAnimatedDrawable(Resources resources, Stream stream) : base(resources, stream)
        {
        }

        public FFAnimatedDrawable(Resources resources, string filePath) : base(resources, filePath)
        {
        }

        public FFAnimatedDrawable(Resources resources, Bitmap bitmap) : base(resources, bitmap)
        {
        }

        public FFAnimatedDrawable(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }

        internal FFAnimatedDrawable(Resources resources, Bitmap bitmap, IAnimatedImage<Bitmap>[] animatedImages) : base(resources, bitmap)
        {
            AnimatedImages = animatedImages;
        }

        public IAnimatedImage<Bitmap>[] AnimatedImages { get; private set; }
    }
}
