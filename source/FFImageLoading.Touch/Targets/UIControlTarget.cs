using System;
using System.Threading.Tasks;
using FFImageLoading.Extensions;
using ObjCRuntime;
using UIKit;

namespace FFImageLoading.Work
{
    public interface IUIViewTarget<TControl>
        where TControl : class, INativeObject
	{
		TControl Control { get; }
	}

	public abstract class UIControlTarget<TControl>: Target<UIImage, ImageLoaderTask>, IUIViewTarget<TControl>
		where TControl: class, INativeObject
	{
		protected readonly WeakReference<TControl> _controlWeakReference;

		protected UIControlTarget(TControl control)
		{
			_controlWeakReference = new WeakReference<TControl>(control);
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
			return IsValid;
		}

		public override bool UsesSameNativeControl(ImageLoaderTask task)
		{
			var otherTarget = task._target as IUIViewTarget<TControl>;
			if (otherTarget == null)
				return false;

			var control = Control;
			var otherControl = otherTarget.Control;
			if (control == null || otherControl == null)
				return false;

			return control.Handle == otherControl.Handle;
		}

		public TControl Control
		{
			get
			{
				TControl control;
				if (!_controlWeakReference.TryGetTarget(out control))
					return null;

				if (control == null || control.Handle == IntPtr.Zero)
					return null;

				return control;
			}
		}
	}
}

