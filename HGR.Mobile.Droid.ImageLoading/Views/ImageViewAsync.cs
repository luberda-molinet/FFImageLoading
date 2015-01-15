using System;
using Android.Content;
using Android.Util;
using HGR.Mobile.Droid.ImageLoading.Work;
using System.Drawing;

namespace HGR.Mobile.Droid.ImageLoading.Views
{
    public abstract class ImageViewAsync : ManagedImageView
    {
        protected SizeF? _predefinedSize;

        public ImageViewAsync(Context context, SizeF? predefinedSize = null) : base(context)
        {
            SetWillNotDraw(false);
        }

        public ImageViewAsync(Context context, IAttributeSet attrs, SizeF? predefinedSize = null) : base(context, attrs)
        {
            SetWillNotDraw(false);
        }

        protected override void OnDetachedFromWindow()
        {
            CancelLoading();
            base.OnDetachedFromWindow();
        }

        public void CancelLoading()
        {
            ImageService.CancelWorkFor(this);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            if (Drawable == null) {
                if (_predefinedSize.HasValue)
                    SetMeasuredDimension((int)_predefinedSize.Value.Width, (int)_predefinedSize.Value.Height);
                else
                    SetMeasuredDimension(widthMeasureSpec, heightMeasureSpec);
            } else {
                base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            }
        }
    }
}