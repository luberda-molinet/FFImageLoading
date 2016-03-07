using System;
using FFImageLoading.Views;
using Android.Content;
using Android.Runtime;
using Android.Util;

namespace FFImageLoading.Forms.Droid
{
	public class CachedImageView : ImageViewAsync
	{
        public CachedImageView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            SetWillNotDraw(false);
        }

        public CachedImageView(Context context) : base(context)
        {
            SetWillNotDraw(false);
        }

        public CachedImageView(Context context, IAttributeSet attrs): base(context, attrs)
        {
            SetWillNotDraw(false);
        }

        [Obsolete]
        public CachedImageView(Context context, System.Drawing.SizeF? predefinedStyle) : base(context)
        {
            SetWillNotDraw(false);
        }

        [Obsolete]
        public CachedImageView(Context context, IAttributeSet attrs, System.Drawing.SizeF? predefinedStyle) : base(context, attrs)
        {
            SetWillNotDraw(false);
        }
	}
}

