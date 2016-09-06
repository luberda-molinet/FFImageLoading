using System;
using Android.Graphics.Drawables;
using Android.Widget;
using System.Threading.Tasks;
using FFImageLoading.Extensions;
using Android.Graphics;

namespace FFImageLoading.Work
{
	public class ImageViewTarget: Target<BitmapDrawable, ImageLoaderTask>
	{
		private readonly WeakReference<ImageView> _controlWeakReference;

		public ImageViewTarget(ImageView control)
		{
			_controlWeakReference = new WeakReference<ImageView>(control);
		}

		public override bool IsValid
		{
			get
			{
				return Control != null;
			}
		}

		public override bool IsTaskValid(ImageLoaderTask task)
		{
			var control = Control;
			if (control == null)
				return false;

			var controlTask = control.GetImageLoaderTask();
			return controlTask == task;
		}

        public override void SetAsEmpty()
        {
            var control = Control;
            if (control == null)
                return;

            control.SetImageResource(global::Android.Resource.Color.Transparent);
        }

		public override void Set(ImageLoaderTask task, BitmapDrawable image, bool isLocalOrFromCache, bool isLoadingPlaceholder)
		{
			if (task.IsCancelled)
				return;
			
			var control = Control;
			if (control == null)
				return;

			control.SetImageDrawable(image);
		}

		public override bool UsesSameNativeControl(ImageLoaderTask task)
		{
			var otherTarget = task._target as ImageViewTarget;
			if (otherTarget == null)
				return false;

			var control = Control;
			var otherControl = otherTarget.Control;
			if (control == null || otherControl == null)
				return false;

			return control.Handle == otherControl.Handle;
		}

		protected ImageView Control
		{
			get
			{
				ImageView control;
				if (!_controlWeakReference.TryGetTarget(out control))
					return null;

				if (control == null || control.Handle == IntPtr.Zero)
					return null;

				return control;
			}
		}
	}
}

