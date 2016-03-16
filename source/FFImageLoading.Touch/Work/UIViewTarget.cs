using System;
using System.Threading.Tasks;
using FFImageLoading.Extensions;
using UIKit;

namespace FFImageLoading.Work
{
	public interface IUIViewTarget
	{
		UIView Control { get; }
	}

	public abstract class UIViewTarget<TView>: Target<UIImage, ImageLoaderTask>, IUIViewTarget
		where TView: UIView
	{
		protected readonly WeakReference<TView> _controlWeakReference;

		protected UIViewTarget(TView control)
		{
			_controlWeakReference = new WeakReference<TView>(control);
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
			var otherTarget = task._target as IUIViewTarget;
			if (otherTarget == null)
				return false;

			var control = Control;
			var otherControl = otherTarget.Control;
			if (control == null || otherControl == null)
				return false;

			return control.Handle == otherControl.Handle;
		}

		UIView IUIViewTarget.Control
		{
			get
			{
				return Control;
			}
		}

		protected TView Control
		{
			get
			{
				TView control;
				if (!_controlWeakReference.TryGetTarget(out control))
					return null;

				if (control == null || control.Handle == IntPtr.Zero)
					return null;

				return control;
			}
		}
	}
}

