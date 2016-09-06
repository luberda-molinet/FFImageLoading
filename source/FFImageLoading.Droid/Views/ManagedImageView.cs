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
		private WeakReference<Drawable> _drawableRef = null;

		public ManagedImageView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
			SetWillNotDraw(false);
		}

		public ManagedImageView(Context context) : base(context)
		{
			SetWillNotDraw(false);
		}

        public ManagedImageView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
            SetWillNotDraw(false);
		}

        public ManagedImageView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            SetWillNotDraw(false);
        }

		protected override void Dispose(bool disposing)
		{
			if (_drawableRef != null)
			{
				Drawable drawable = null; 

				if (_drawableRef.TryGetTarget(out drawable))
				{
					UpdateDrawableDisplayedState(drawable, false);
				}	

				_drawableRef = null;
			}

			base.Dispose(disposing);
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

			_drawableRef = new WeakReference<Drawable>(drawable);
			base.SetImageDrawable(drawable);

			UpdateDrawableDisplayedState(drawable, true);
			UpdateDrawableDisplayedState(previous, false);
		}

		public override void SetImageResource(int resId)
		{
			var previous = Drawable;
			// Ultimately calls SetImageDrawable, where the state will be updated.
			_drawableRef = null;
			base.SetImageResource(resId);
			UpdateDrawableDisplayedState(previous, false);
		}

		public override void SetImageURI(global::Android.Net.Uri uri)
		{
			var previous = Drawable;
			// Ultimately calls SetImageDrawable, where the state will be updated.
			_drawableRef = null;
			base.SetImageURI(uri);
			UpdateDrawableDisplayedState(previous, false);
		}

		private void UpdateDrawableDisplayedState(Drawable drawable, bool isDisplayed)
		{
			if (drawable == null || drawable.Handle == IntPtr.Zero)
				return;

			var selfDisposingBitmapDrawable = drawable as SelfDisposingBitmapDrawable;
			if (selfDisposingBitmapDrawable != null)
			{
				if (selfDisposingBitmapDrawable.HasValidBitmap)
					selfDisposingBitmapDrawable.SetIsDisplayed(isDisplayed);
			}
			else
			{
				var layerDrawable = drawable as LayerDrawable;
				if (layerDrawable != null)
				{
					for (var i = 0; i < layerDrawable.NumberOfLayers; i++)
					{
						UpdateDrawableDisplayedState(layerDrawable.GetDrawable(i), isDisplayed);
					}
				}
			}
		}
	}

}