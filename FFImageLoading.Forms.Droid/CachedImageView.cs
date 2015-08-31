using System;
using FFImageLoading.Views;
using Android.Content;

namespace FFImageLoading.Forms.Droid
{
	public class CachedImageView : ImageViewAsync
	{
		private bool _skipInvalidate;

		public CachedImageView(Context context) : base(context)
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

