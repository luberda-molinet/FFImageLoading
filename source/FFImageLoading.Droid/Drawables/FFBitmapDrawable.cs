using System;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.OS;
using Android.Content.Res;
using Android.Graphics;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace FFImageLoading.Drawables
{
    public class FFBitmapDrawable : SelfDisposingBitmapDrawable
    {
        BitmapDrawable placeholder;
        long startTimeMillis;
        bool animating;
        int alpha = 255;
        float fadeDuration = 200;

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

        public void SetPlaceholder(BitmapDrawable drawable, int animationDuration)
        {
            if (!animating)
            {
                alpha = 255;
                fadeDuration = animationDuration;
                startTimeMillis = SystemClock.UptimeMillis();
                placeholder = drawable;
                animating = true;
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
                    float normalized = (SystemClock.UptimeMillis() - startTimeMillis) / fadeDuration;
                    if (normalized >= 1f)
                    {
                        animating = false;
                        placeholder = null;
                        normalized = 0f;
                        base.Draw(canvas);
                    }
                    else
                    {
                        if (IsBitmapDrawableValid(placeholder))
                        {
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

        bool IsBitmapDrawableValid(BitmapDrawable bitmapDrawable)
        {
            return bitmapDrawable != null && bitmapDrawable.Handle != IntPtr.Zero && bitmapDrawable.Bitmap != null
                                  && bitmapDrawable.Handle != IntPtr.Zero && !bitmapDrawable.Bitmap.IsRecycled;
        }

        public override int IntrinsicHeight
        {
            get
            {
                try
                {
                    if (animating && IsBitmapDrawableValid(placeholder))
                    {
                        return placeholder.IntrinsicHeight;
                    }
                }
                catch (Exception) { }

                return base.IntrinsicHeight;
            }
        }

        public override int IntrinsicWidth
        {
            get
            {
                try
                {
                    if (animating && IsBitmapDrawableValid(placeholder))
                    {
                        return placeholder.IntrinsicWidth;
                    }
                }
                catch (Exception) {}

                return base.IntrinsicWidth;
            }
        }


        public override void SetAlpha(int alpha)
        {
            try
            {
                if (IsBitmapDrawableValid(placeholder))
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
                if (IsBitmapDrawableValid(placeholder))
                {
                    placeholder.SetColorFilter(color, mode);
                }

                base.SetColorFilter(color, mode);
            }
            catch (Exception) { }
        }
    }
}
