using System;
using FFImageLoading.Extensions;
using FFImageLoading.Drawables;
using FFImageLoading.Views;
using FFImageLoading.Work;
using Android.Graphics.Drawables;

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
                return Control != null && Control.Handle != IntPtr.Zero;
            }
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

            var isLayoutNeeded = IsLayoutNeeded(task, control.Drawable, image);

            control.SetImageDrawable(image);
            control.Invalidate();

            if (isLayoutNeeded)
                control.RequestLayout();
        }

        bool IsLayoutNeeded(IImageLoaderTask task, Drawable oldImage, Drawable newImage)
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

