using Android.Content.Res;
using Android.Graphics;
using System;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Runtime;
using System.IO;
using FFImageLoading.Work;
using FFImageLoading.Helpers;

namespace FFImageLoading.Drawables
{
    public class FFGifDrawable : SelfDisposingBitmapDrawable
    {
        public FFGifDrawable() : base()
        {
        }

        public FFGifDrawable(Resources resources) : base(resources)
        {
        }

        public FFGifDrawable(Resources resources, Stream stream) : base(resources, stream)
        {
        }

        public FFGifDrawable(Resources resources, string filePath) : base(resources, filePath)
        {
        }

        public FFGifDrawable(Bitmap bitmap) : base(bitmap)
        {
        }

        public FFGifDrawable(Stream stream) : base(stream)
        {
        }

        public FFGifDrawable(string filePath) : base(filePath)
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
