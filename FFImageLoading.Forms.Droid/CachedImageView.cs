using System;
using FFImageLoading.Views;
using Android.Content;
using Android.Runtime;
using Android.Util;

namespace FFImageLoading.Forms.Droid
{
	public class CachedImageView : ImageViewAsync
	{
		private bool _skipInvalidate;

		public CachedImageView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
		}

		public CachedImageView(Context context) : base(context)
		{
		}

		public CachedImageView(Context context, IAttributeSet attrs): base(context, attrs)
		{
		}

		public CachedImageView(Context context, System.Drawing.SizeF? predefinedStyle) : base(context, predefinedStyle)
		{
		}

		public CachedImageView(Context context, IAttributeSet attrs, System.Drawing.SizeF? predefinedStyle) : base(context, attrs, predefinedStyle)
		{
		}

		public override void Invalidate()
		{
			if (this._skipInvalidate)
			{
				this._skipInvalidate = false;
				return;
			}
			base.Invalidate();
		}

		public void SkipInvalidate()
		{
			this._skipInvalidate = true;
		}
	}

}

