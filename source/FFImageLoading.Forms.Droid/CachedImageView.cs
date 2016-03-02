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
		}

		public CachedImageView(Context context) : base(context)
		{
		}

		public CachedImageView(Context context, IAttributeSet attrs): base(context, attrs)
		{
		}

        [Obsolete]
        public CachedImageView(Context context, System.Drawing.SizeF? predefinedStyle) : base(context)
		{
		}

        [Obsolete]
        public CachedImageView(Context context, IAttributeSet attrs, System.Drawing.SizeF? predefinedStyle) : base(context, attrs)
		{
		}
	}
}

