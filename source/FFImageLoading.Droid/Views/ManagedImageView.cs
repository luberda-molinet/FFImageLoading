using Android.Content;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;
using System;
using Android.Runtime;
using FFImageLoading.Drawables;

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
			var previous = Drawable;

			base.SetImageDrawable(drawable);

			UpdateDrawableDisplayedState(drawable, true);
			UpdateDrawableDisplayedState(previous, false);
		}

		public override void SetImageResource(int resId)
		{
			var previous = Drawable;
			// Ultimately calls SetImageDrawable, where the state will be updated.
			base.SetImageResource(resId);
			UpdateDrawableDisplayedState(previous, false);
		}

		public override void SetImageURI(global::Android.Net.Uri uri)
		{
			var previous = Drawable;
			// Ultimately calls SetImageDrawable, where the state will be updated.
			base.SetImageURI(uri);
			UpdateDrawableDisplayedState(previous, false);
		}

		private void UpdateDrawableDisplayedState(Drawable drawable, bool isDisplayed)
		{
			if (drawable is SelfDisposingBitmapDrawable) {
				((SelfDisposingBitmapDrawable)drawable).SetIsDisplayed(isDisplayed);
			} else if (drawable is LayerDrawable) {
				var layerDrawable = (LayerDrawable)drawable;
				for (var i = 0; i < layerDrawable.NumberOfLayers; i++) {
					UpdateDrawableDisplayedState(layerDrawable.GetDrawable(i), isDisplayed);
				}
			}
		}
	}
}