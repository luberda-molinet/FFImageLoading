using Android.Content.Res;
using Android.Graphics;
using System;
using Android.Runtime;
using System.IO;

namespace FFImageLoading.Drawables
{
    public class FFGifDrawable : SelfDisposingBitmapDrawable
    {
        public FFGifDrawable(Resources resources, Stream stream) : base(resources, stream)
        {
        }

        public FFGifDrawable(Resources resources, string filePath) : base(resources, filePath)
        {
        }

        public FFGifDrawable(Resources resources, Bitmap bitmap) : base(resources, bitmap)
        {
        }

        public FFGifDrawable(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }

        internal FFGifDrawable(Resources resources, Bitmap bitmap, IAnimatedImage<Bitmap>[] animatedImages) : base(resources, bitmap)
        {
            AnimatedImages = animatedImages;
        }

        internal IAnimatedImage<Bitmap>[] AnimatedImages { get; private set; }
    }
}
