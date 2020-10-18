using System;
using Android.Graphics.Drawables;
using FFImageLoading.Drawables;
using FFImageLoading.Work;

namespace FFImageLoading.Targets
{
	public abstract class ViewTarget<TView> : Target<SelfDisposingBitmapDrawable, TView> where TView : Android.Views.View
	{
		protected readonly WeakReference<TView> _controlWeakReference;

		protected ViewTarget(TView control)
		{
			_controlWeakReference = new WeakReference<TView>(control);
		}

		public override bool IsValid
		{
			get
			{
				try
				{
					return Control != null && Control.Handle != IntPtr.Zero;
				}
				catch (ObjectDisposedException)
				{
					return false;
				}
			}
		}

		protected static void UpdateDrawableDisplayedState(Drawable drawable, bool isDisplayed)
		{
			if (drawable == null || drawable.Handle == IntPtr.Zero)
				return;

			if (drawable is ISelfDisposingBitmapDrawable selfDisposingBitmapDrawable)
			{
				if (selfDisposingBitmapDrawable.HasValidBitmap)
					selfDisposingBitmapDrawable.SetIsDisplayed(isDisplayed);
			}
			else
			{
				if (drawable is LayerDrawable layerDrawable)
				{
					for (var i = 0; i < layerDrawable.NumberOfLayers; i++)
					{
						UpdateDrawableDisplayedState(layerDrawable.GetDrawable(i), isDisplayed);
					}
				}
			}
		}

		protected bool IsLayoutNeeded(IImageLoaderTask task, Drawable oldImage, Drawable newImage)
		{
			if (task.Parameters.InvalidateLayoutEnabled.HasValue)
			{
				if (!task.Parameters.InvalidateLayoutEnabled.Value)
					return false;
			}
			else if (!task.Configuration.InvalidateLayout)
			{
				return false;
			}

			try
			{
				if (oldImage == null && newImage == null)
					return false;

				if (oldImage == null && newImage != null)
					return true;

				if (oldImage != null && newImage == null)
					return true;

				if (oldImage != null && newImage != null)
				{
					return !(oldImage.IntrinsicWidth == newImage.IntrinsicWidth && oldImage.IntrinsicHeight == newImage.IntrinsicHeight);
				}
			}
			catch (Exception)
			{
				return true;
			}

			return false;
		}

		public override TView Control
		{
			get
			{
				if (!_controlWeakReference.TryGetTarget(out var control))
					return null;

				if (control == null || control.Handle == IntPtr.Zero)
					return null;

				return control;
			}
		}
	}
}
