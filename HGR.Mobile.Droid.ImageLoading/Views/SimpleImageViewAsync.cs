using System;
using Android.Util;
using HGR.Mobile.Droid.ImageLoading.Work;
using System.Drawing;
using Android.Content;

namespace HGR.Mobile.Droid.ImageLoading.Views
{
    public class SimpleImageViewAsync: ImageViewAsyncBase<string>
    {
        protected SizeF? _predefinedSize;

        public SimpleImageViewAsync(Context context, SizeF? predefinedSize = null) : base(context)
        {
            _predefinedSize = predefinedSize;
            SetWillNotDraw(false);
        }

        public SimpleImageViewAsync(Context context, IAttributeSet attrs, SizeF? predefinedSize = null) : base(context, attrs)
        {
            _predefinedSize = predefinedSize;
            SetWillNotDraw(false);
        }

        protected override ImageWorkerBase<string> ImageWorker
        {
            get
            {
                return HGR.Mobile.Droid.ImageLoading.Work.ImageWorker.Instance;
            }
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            if (Drawable == null && _predefinedSize.HasValue)
                SetMeasuredDimension((int) _predefinedSize.Value.Width, (int) _predefinedSize.Value.Height);
            else
                base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
        }
    }
}

