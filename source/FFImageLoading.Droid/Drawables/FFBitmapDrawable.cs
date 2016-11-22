using System;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.OS;
using Android.Content.Res;
using Android.Graphics;
using System.IO;
using FFImageLoading.Work;

namespace FFImageLoading.Drawables
{
    public class FFBitmapDrawable : SelfDisposingBitmapDrawable
    {
        WeakReference<ISelfDisposingBitmapDrawable> baseDrawable;
        BitmapDrawable placeholder;
        long startTimeMillis;
        bool animating;
        int alpha = 255;
        float fadeDuration = 200;
        bool placeholderInitialized;
        Rect orgRect;

        public FFBitmapDrawable(Resources res, Bitmap bitmap, SelfDisposingBitmapDrawable baseDrawable) : base(res, bitmap)
        {
            this.baseDrawable = new WeakReference<ISelfDisposingBitmapDrawable>(baseDrawable);
        }

        public FFBitmapDrawable(Resources res, Bitmap bitmap) : base(res, bitmap)
        {
        }

        public FFBitmapDrawable() : base()
        {
        }

        public FFBitmapDrawable(Resources resources) : base(resources)
        {
        }

        public FFBitmapDrawable(Resources resources, Stream stream) : base(resources, stream)
        {
        }

        public FFBitmapDrawable(Resources resources, string filePath) : base(resources, filePath)
        {
        }

        public FFBitmapDrawable(Bitmap bitmap) : base(bitmap)
        {
        }

        public FFBitmapDrawable(Stream stream) : base(stream)
        {
        }

        public FFBitmapDrawable(string filePath) : base(filePath)
        {
        }

        public FFBitmapDrawable(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }

        public void SetPlaceholder(SelfDisposingBitmapDrawable drawable, int animationDuration)
        {
            if (!animating)
            {
                alpha = 255;
                fadeDuration = animationDuration;
                startTimeMillis = SystemClock.UptimeMillis();
                placeholder = drawable?.GetConstantState().NewDrawable() as BitmapDrawable;
                animating = true;
            }
        }

        public override void SetIsDisplayed(bool isDisplayed)
        {
            base.SetIsDisplayed(isDisplayed);

            if (baseDrawable != null)
            {
                ISelfDisposingBitmapDrawable sdbDraw = null;
                if (baseDrawable.TryGetTarget(out sdbDraw) && sdbDraw.IsValidAndHasValidBitmap())
                {
                    sdbDraw.SetIsDisplayed(isDisplayed);
                }
            }
        }

        public bool IsAnimationRunning
        {
            get { return animating; }
        }

        public int FadeDuration
        {
            get { return (int)fadeDuration; }
        }

        protected override void OnBoundsChange(Rect bounds)
        {
            orgRect = bounds;
            placeholderInitialized = false;
            base.OnBoundsChange(bounds);
        }

        public override void Draw(Canvas canvas)
        {
            try
            {
                if (!animating)
                {
                    base.Draw(canvas);
                }
                else
                {
                    var normalized = (SystemClock.UptimeMillis() - startTimeMillis) / fadeDuration;
                    if (normalized >= 1f)
                    {
                        animating = false;
                        placeholder = null;
                        normalized = 1f;
                        base.Draw(canvas);
                    }
                    else
                    {
                        if (placeholder.IsValidAndHasValidBitmap())
                        {
                            if (!placeholderInitialized)
                            {
                                var placeholderSizeRatio = canvas.Width > canvas.Height ?
                                                                 (double)orgRect.Right / placeholder.IntrinsicWidth
                                                                 : (double)orgRect.Bottom / placeholder.IntrinsicHeight;

                                var scaledWidth = placeholderSizeRatio * placeholder.IntrinsicWidth;
                                var newOffset = (double)orgRect.CenterX() - scaledWidth / 2;
                                placeholder.Gravity = Android.Views.GravityFlags.Fill;
                                placeholder.SetBounds((int)newOffset, orgRect.Top, orgRect.Right, orgRect.Bottom);

                                placeholderInitialized = true;
                            }

                            placeholder.Draw(canvas);
                        }

                        int partialAlpha = (int)(alpha * normalized);
                        base.SetAlpha(partialAlpha);
                        base.Draw(canvas);
                        base.SetAlpha(alpha);
                    }
                }
            }
            catch (Exception) { }
        }

        public override void SetAlpha(int alpha)
        {
            try
            {
                if (placeholder.IsValidAndHasValidBitmap())
                {
                    placeholder.SetAlpha(alpha);
                }

                base.SetAlpha(alpha);
            }
            catch (Exception) { }
        }

        public override void SetColorFilter(Color color, PorterDuff.Mode mode)
        {
            try
            {
                if (placeholder.IsValidAndHasValidBitmap())
                {
                    placeholder.SetColorFilter(color, mode);
                }

                base.SetColorFilter(color, mode);
            }
            catch (Exception) { }
        }
    }
}
