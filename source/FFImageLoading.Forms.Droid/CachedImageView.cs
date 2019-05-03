using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Graphics.Drawables;
using Android.Widget;

namespace FFImageLoading.Forms.Platform
{
    [Preserve(AllMembers = true)]
    public class CachedImageView : ImageView
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

