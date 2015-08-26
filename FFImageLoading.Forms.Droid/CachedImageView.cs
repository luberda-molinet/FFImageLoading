using System;
using FFImageLoading.Views;
using Android.Content;

namespace FFImageLoading.Forms.Droid
{
	public class CachedImageView : ImageViewAsync
	{
		private bool skipInvalidate;

		public CachedImageView(Context context) : base(context)
		{
		}

		public override void Invalidate()
		{
			if (this.skipInvalidate)
			{
				this.skipInvalidate = false;
				return;
			}
			base.Invalidate();
		}

		public void SkipInvalidate()
		{
			this.skipInvalidate = true;
		}
	}

}

