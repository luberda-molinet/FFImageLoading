using Android.Content;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;
using System;
using Android.Runtime;

namespace FFImageLoading.Views
{
	public class ManagedImageView : ImageView
	{
        public ManagedImageView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

		public ManagedImageView(Context context) : base(context, null)
		{
		}

		public ManagedImageView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			SetWillNotDraw(false);
		}

		/* FMT: this is not fine when working with RecyclerView... It can detach and cache the view, then reattach it
		protected override void OnDetachedFromWindow()
		{
			SetImageDrawable(null);
			base.OnDetachedFromWindow();
		}
		*/

		public override void SetImageDrawable(Drawable drawable)
		{
			var previousDrawable = Drawable;

			base.SetImageDrawable(drawable);

			NotifyDrawable(previousDrawable, false);
			NotifyDrawable(drawable, true);
		}

		private static void NotifyDrawable(Drawable drawable, bool isDisplayed)
		{
			var layerDrawable = drawable as LayerDrawable;
			if (layerDrawable != null)
			{
				for (var i = 0; i < layerDrawable.NumberOfLayers; i++)
				{
					NotifyDrawable(layerDrawable.GetDrawable(i), isDisplayed);
				}
			}
		}
	}
}