using Android.Content;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;
using FFImageLoading.Drawables;

namespace FFImageLoading.Views
{
	public class ManagedImageView : ImageView
	{
		public ManagedImageView(Context context) : base(context, null)
		{
		}

		public ManagedImageView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			SetWillNotDraw(false);
		}

		protected override void OnDetachedFromWindow()
		{
			SetImageDrawable(null);
			base.OnDetachedFromWindow();
		}

		public override void SetImageDrawable(Drawable drawable)
		{
			var previousDrawable = Drawable;

			base.SetImageDrawable(drawable);

			NotifyDrawable(previousDrawable, false);
			NotifyDrawable(drawable, true);
		}

		private static void NotifyDrawable(Drawable drawable, bool isDisplayed)
		{
			var bitmapDrawable = drawable as ManagedBitmapDrawable;
			if (bitmapDrawable != null)
			{
				bitmapDrawable.SetIsDisplayed(isDisplayed);
			}
			else
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
}