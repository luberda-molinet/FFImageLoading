using System;
using FFImageLoading.Extensions;
using FFImageLoading.Drawables;
using FFImageLoading.Views;
using FFImageLoading.Work;

namespace FFImageLoading.Targets
{
    public class ImageViewTarget : Target<SelfDisposingBitmapDrawable, ImageViewAsync>
	{
		readonly WeakReference<ImageViewAsync> _controlWeakReference;

		public ImageViewTarget(ImageViewAsync control)
		{
			_controlWeakReference = new WeakReference<ImageViewAsync>(control);
		}

		public override bool IsValid
		{
			get
			{
				return Control != null;
			}
		}

		public override bool IsTaskValid(IImageLoaderTask task)
		{
            var controlTask = Control?.GetImageLoaderTask();
            return IsValid && (controlTask == null || controlTask == task);
		}

        public override void SetAsEmpty(IImageLoaderTask task)
        {
            if (task == null || task.IsCancelled)
                return;
            
            var control = Control;
            if (control == null)
                return;

            control.SetImageResource(global::Android.Resource.Color.Transparent);
        }

        public override void Set(IImageLoaderTask task, SelfDisposingBitmapDrawable image, bool animated)
        {
            if (task == null || task.IsCancelled)
                return;

            var control = Control;
            if (control == null || control.Drawable == image)
                return;

            control.SetImageDrawable(image);
            control.PostInvalidate();
        }

		public override bool UsesSameNativeControl(IImageLoaderTask task)
		{
            var otherTarget = task.Target as ImageViewTarget;
			if (otherTarget == null)
				return false;

			var control = Control;
			var otherControl = otherTarget.Control;
			if (control == null || otherControl == null)
				return false;

			return control.Handle == otherControl.Handle;
		}

        public override ImageViewAsync Control
		{
			get
			{
				ImageViewAsync control;
				if (!_controlWeakReference.TryGetTarget(out control))
					return null;

				if (control == null || control.Handle == IntPtr.Zero)
					return null;

                return control;
			}
		}
	}
}

