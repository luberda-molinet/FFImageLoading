using System;
using Android.Content;
using Android.Util;
using HGR.Mobile.Droid.ImageLoading.Work;

namespace HGR.Mobile.Droid.ImageLoading.Views
{
    public abstract class ImageViewAsyncBase<TKey> : ManagedImageView
    {
        protected ImageViewAsyncBase(Context context) : base(context)
        {
        }

        protected ImageViewAsyncBase(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        protected abstract ImageWorkerBase<TKey> ImageWorker { get; }

        protected override void OnDetachedFromWindow()
        {
            CancelLoading();
            base.OnDetachedFromWindow();
        }

        public void CancelLoading()
        {
            ImageWorker.Cancel(this);
        }


        public virtual void SetImage(TKey key, Action onComplete = null)
        {
            ImageWorker.LoadImage(key, this, onComplete);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            if (Drawable == null)
                base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            else
                SetMeasuredDimension(LayoutParameters.Width, LayoutParameters.Height);
        }
    }
}