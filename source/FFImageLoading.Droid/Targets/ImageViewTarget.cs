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
		private static readonly Dictionary<int, HighResolutionTimer<ISelfDisposingAnimatedBitmapDrawable>> _runningAnimations = new Dictionary<int, HighResolutionTimer<ISelfDisposingAnimatedBitmapDrawable>>();

        public ImageViewTarget(ImageView control) : base(control)
        {
        }

		private static void PlayAnimation(ImageView control, ISelfDisposingAnimatedBitmapDrawable drawable)
		{
			lock (_runningAnimations)
			{
				var hashCode = control.GetHashCode();

				var timer = new HighResolutionTimer<ISelfDisposingAnimatedBitmapDrawable>(drawable, async (t, bitmap) =>
				{
					try
					{
						try
						{
							if (control == null || control.Handle == IntPtr.Zero || !t.Enabled
								|| bitmap == null || bitmap.Handle == IntPtr.Zero || bitmap.IsRecycled
								|| !t.AnimatedDrawable.IsValidAndHasValidBitmap())
							{
								return;
							}

							await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() =>
							{
								if (control == null || control.Handle == IntPtr.Zero || !t.Enabled
									|| bitmap == null || bitmap.Handle == IntPtr.Zero || bitmap.IsRecycled
									|| !t.AnimatedDrawable.IsValidAndHasValidBitmap())
								{
									return;
								}

								control.SetImageBitmap(bitmap);
							}).ConfigureAwait(false);
						}
						catch (ObjectDisposedException)
						{
							StopAnimation(hashCode);
						}
					}
					catch (Exception ex)
					{
						ImageService.Instance.Config.Logger.Error("GIF", ex);
					}
				})
				{
					DelayOffset = -2
				};

				_runningAnimations.Add(hashCode, timer);

				timer.Start();
			}
		}

		private static void StopAnimation(int hashCode)
		{
			lock (_runningAnimations)
			{
				if (_runningAnimations.TryGetValue(hashCode, out var timer))
				{
					_runningAnimations.Remove(hashCode);
					timer?.Stop();
					UpdateDrawableDisplayedState(timer.AnimatedDrawable as Drawable, false);
				}
			}
		}

		private static void Set(ImageView control, SelfDisposingBitmapDrawable drawable)
		{
			lock (control)
			{
				StopAnimation(control.GetHashCode());

				if (drawable == null)
				{
					control.SetImageResource(Android.Resource.Color.Transparent);
					return;
				}

				if (drawable is ISelfDisposingAnimatedBitmapDrawable animatedBitmapDrawable)
				{
					UpdateDrawableDisplayedState(drawable, true);
					control.SetImageDrawable(animatedBitmapDrawable as Drawable);
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

