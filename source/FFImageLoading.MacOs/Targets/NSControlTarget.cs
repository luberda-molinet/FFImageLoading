using System;
using FFImageLoading.Work;
using ObjCRuntime;
using AppKit;

namespace FFImageLoading.Targets
{
	public abstract class NSControlTarget<TControl> : Target<NSImage, TControl> where TControl: class, INativeObject
	{
		protected readonly WeakReference<TControl> _controlWeakReference;

		protected NSControlTarget(TControl control)
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

		public override bool IsTaskValid(IImageLoaderTask task)
		{
			return IsValid;
		}

		public override bool UsesSameNativeControl(IImageLoaderTask task)
		{
            var otherTarget = task.Target as NSControlTarget<TControl>;
			if (otherTarget == null)
				return false;

			var control = Control;
			var otherControl = otherTarget.Control;
			if (control == null || otherControl == null)
				return false;

			return control.Handle == otherControl.Handle;
		}

		public override TControl Control
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

