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

        public virtual void SetFromFile(string filepath, Action onComplete = null, int resampleWidth = -1, int resampleHeight = -1)
        {
            var task = new ImageLoaderTask(filepath, this, ImageLoaderTask.ImageSource.Filepath, resampleWidth, resampleHeight);
            ImageService.LoadImage(filepath, task, this, onComplete);
        }

        public virtual void SetFromUrl(string url, Action onComplete = null, int resampleWidth = -1, int resampleHeight = -1, TimeSpan? duration = null)
        {
            var task = new ImageLoaderTask(url, this, ImageLoaderTask.ImageSource.Url, resampleWidth, resampleHeight);
            ImageService.LoadImage(url, task, this, onComplete);
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