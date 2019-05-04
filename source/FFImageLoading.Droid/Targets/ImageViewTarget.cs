using System;
using FFImageLoading.Drawables;
using FFImageLoading.Work;
using Android.Graphics.Drawables;
using Android.Widget;
using System.Linq;
using System.Collections.Generic;

namespace FFImageLoading.Targets
{
    public class ImageViewTarget : ViewTarget<ImageView>
    {
		private static readonly Dictionary<ImageView, HighResolutionTimer<Android.Graphics.Bitmap>> _runningAnimations = new Dictionary<ImageView, HighResolutionTimer<Android.Graphics.Bitmap>>();

        public ImageViewTarget(ImageView control) : base(control)
        {
        }

		private static void PlayAnimation(ImageView control, ISelfDisposingAnimatedBitmapDrawable drawable)
		{
			lock (_runningAnimations)
			{
				StopAnimation(control);

				var timer = new HighResolutionTimer<Android.Graphics.Bitmap>(drawable, async (t, bitmap) =>
				{
					try
					{
						if (control == null || control.Handle == IntPtr.Zero || !t.Enabled
							|| bitmap == null || bitmap.Handle == IntPtr.Zero || bitmap.IsRecycled)
						{
							t.Stop();
							return;
						}

						await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() =>
						{
							if (control == null || control.Handle == IntPtr.Zero || !t.Enabled
								|| bitmap == null || bitmap.Handle == IntPtr.Zero || bitmap.IsRecycled)
							{
								t.Stop();
								return;
							}

							control.SetImageBitmap(bitmap);
						}).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						ImageService.Instance.Config.Logger.Error("GIF", ex);
					}
				})
				{
					DelayOffset = -2
				};

				_runningAnimations.Add(control, timer);

				timer.Start();
			}
		}

		private static void StopAnimation(ImageView control)
		{
			lock (_runningAnimations)
			{
				if (_runningAnimations.TryGetValue(control, out var timer))
				{
					timer?.Stop();
					_runningAnimations.Remove(control);
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
    }
}

