using System;
using FFImageLoading.Drawables;
using FFImageLoading.Work;
using Android.Graphics.Drawables;
using Android.Widget;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FFImageLoading.Helpers;
using Java.Util.Logging;

namespace FFImageLoading.Targets
{
    public class ImageViewTarget : ViewTarget<ImageView>
    {
		private static readonly ConditionalWeakTable<ImageView, HighResolutionTimer<ISelfDisposingAnimatedBitmapDrawable>> _runningAnimations = new ConditionalWeakTable<ImageView, HighResolutionTimer<ISelfDisposingAnimatedBitmapDrawable>>();

        public ImageViewTarget(ImageView control, IMiniLogger logger) : base(control)
        {
			Logger = logger;
        }

		protected readonly IMiniLogger Logger;

		private void PlayAnimation(ImageView control, ISelfDisposingAnimatedBitmapDrawable drawable)
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
							if (control == null || control.Handle == IntPtr.Zero
							|| !drawable.IsValidAndHasValidBitmap())
							{
								StopAnimation(control);
								return;
							}

							control.Handler.Post(() =>
							{
								if (control == null || control.Handle == IntPtr.Zero
								|| !drawable.IsValidAndHasValidBitmap())
								{
									StopAnimation(control);
									return;
								}

								control.SetImageBitmap(bitmap);
							});
						}
						catch (ObjectDisposedException)
						{
							StopAnimation(control);
						}
					}
					catch (Exception ex)
					{
						Logger.Error("GIF", ex);
					}
				})
				{
					DelayOffset = -2
				};

				_runningAnimations.Add(control, timer);

				timer.Start();
			}
		}

		private static void StopAnimation(ImageView imageView)
		{
			lock (_runningAnimations)
			{
				if (_runningAnimations.TryGetValue(imageView, out var timer))
				{
					timer?.Stop();
					_runningAnimations.Remove(imageView);
					UpdateDrawableDisplayedState(timer.AnimatedDrawable as Drawable, false);
					imageView.SetImageResource(Android.Resource.Color.Transparent);
				}
			}
		}

		private void Set(ImageView control, SelfDisposingBitmapDrawable drawable)
		{
			lock (control)
			{
				StopAnimation(control);

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

