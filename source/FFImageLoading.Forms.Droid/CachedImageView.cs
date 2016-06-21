using System;
using FFImageLoading.Views;
using Android.Content;
using Android.Runtime;
using Android.Util;

namespace FFImageLoading.Forms.Droid
{
	[Preserve(AllMembers = true)]
	public class CachedImageView : ImageViewAsync
	{
		bool _skipInvalidate;

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

