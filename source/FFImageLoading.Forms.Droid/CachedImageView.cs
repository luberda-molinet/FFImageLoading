using System;
using FFImageLoading.Views;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Graphics.Drawables;

namespace FFImageLoading.Forms.Platform
{
    [Preserve(AllMembers = true)]
    public class CachedImageView : ImageViewAsync
    {
        bool _skipInvalidate;

        public CachedImageView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CachedImageView(Context context) : base(context)
        {
        }

        public CachedImageView(Context context, IAttributeSet attrs): base(context, attrs)
        {
        }


        public override void Invalidate()
        {
            if (_skipInvalidate)
            {
                _skipInvalidate = false;
                return;
            }

            base.Invalidate();
        }

        public void SkipInvalidate()
        {
            _skipInvalidate = true;
        }
    }
}

