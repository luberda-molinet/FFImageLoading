using System;
using FFImageLoading.Drawables;
using FFImageLoading.Work;
using Android.Graphics.Drawables;
using Android.Widget;
using System.Linq;

namespace FFImageLoading.Targets
{
    public class ImageViewTarget : Target<SelfDisposingBitmapDrawable, ImageView>
    {
        private readonly WeakReference<ImageView> _controlWeakReference;

        public ImageViewTarget(ImageView control)
        {
            _controlWeakReference = new WeakReference<ImageView>(control);
        }

		private static void PlayAnimation(ImageView control, ISelfDisposingAnimatedBitmapDrawable drawable)
		{

		}

		private static void StopAnimation(ImageView control)
		{

		}

		//private void StopAnimation()
		//{
		//	try
		//	{
		//		_animationTimer?.Stop();
		//		_tcs?.Cancel();
		//	}
		//	catch (ObjectDisposedException) { }
		//}

		//private void PlayAnimation(FFGifDrawable gifDrawable, CancellationTokenSource tokenSource)
		//{
		//	var token = tokenSource.Token;
		//	var animatedImages = gifDrawable.AnimatedImages;

		//	_animationTimer?.Stop();
		//	_animationTimer = new HighResolutionTimer<Android.Graphics.Bitmap>(gifDrawable.AnimatedImages, async (image) =>
		//	{
		//		if (_animationFrameSetting)
		//			return;

		//		_animationFrameSetting = true;

		//		try
		//		{
		//			var bitmap = image.Image;

		//			if (_isDisposed || !_animationTimer.Enabled)
		//				return;

		//			if (bitmap != null && bitmap.Handle != IntPtr.Zero && !bitmap.IsRecycled)
		//			{
		//				if (_isDisposed || !_animationTimer.Enabled)
		//					return;

		//				await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() =>
		//				{
		//					if (_isDisposed || !_animationTimer.Enabled)
		//						return;

		//					base.SetImageBitmap(bitmap);
		//				}).ConfigureAwait(false);
		//			}
		//		}
		//		catch (Exception ex)
		//		{
		//			ImageService.Instance.Config.Logger.Error("GIF", ex);
		//		}
		//		finally
		//		{
		//			_animationFrameSetting = false;
		//		}
		//	})
		//	{
		//		DelayOffset = -2
		//	};
		//	_animationTimer.Start();
		//}

		private static void UpdateDrawableDisplayedState(Drawable drawable, bool isDisplayed)
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

		private static void Set(ImageView control, SelfDisposingBitmapDrawable drawable)
		{
			lock (control)
			{
				if (control.Drawable is ISelfDisposingBitmapDrawable previous)
				{
					if (previous is ISelfDisposingAnimatedBitmapDrawable)
					{
						StopAnimation(control);
					}

					UpdateDrawableDisplayedState(previous as Drawable, false);
				}

				if (drawable == null)
				{
					control.SetImageResource(Android.Resource.Color.Transparent);
					return;
				}
				else if (drawable is ISelfDisposingAnimatedBitmapDrawable animatedBitmapDrawable)
				{
					UpdateDrawableDisplayedState(drawable, true);
					control.SetImageBitmap(animatedBitmapDrawable.AnimatedImages.FirstOrDefault()?.Image);
					PlayAnimation(control, animatedBitmapDrawable);
				}
				else
				{
					UpdateDrawableDisplayedState(drawable, true);
					control.SetImageDrawable(drawable);
				}
			}
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

        public override void SetAsEmpty(IImageLoaderTask task)
        {
            if (task == null || task.IsCancelled)
                return;

            var control = Control;
            if (control == null)
                return;

			Set(control, null);
		}

        public override void Set(IImageLoaderTask task, SelfDisposingBitmapDrawable image, bool animated)
        {
            if (task == null || task.IsCancelled)
                return;
            
            var control = Control;
            if (control == null || control.Drawable == image)
                return;

            var isLayoutNeeded = IsLayoutNeeded(task, control.Drawable, image);

			Set(control, image);
            control.Invalidate();

            if (isLayoutNeeded)
                control.RequestLayout();
        }

        private bool IsLayoutNeeded(IImageLoaderTask task, Drawable oldImage, Drawable newImage)
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

        public override ImageView Control
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

