using System;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.OS;
using Android.Content.Res;
using Android.Graphics;
using System.IO;

namespace FFImageLoading.Drawables
{
    public class FFBitmapDrawable : SelfDisposingBitmapDrawable
    {
        private readonly WeakReference<ISelfDisposingBitmapDrawable> _baseDrawable;
        private BitmapDrawable _placeholder;
        private long _startTimeMillis;
        private int _alpha = 255;
        private float _fadeDuration = 200;
        private bool _placeholderInitialized;
        private Rect _orgRect;

        public FFBitmapDrawable(Resources res, Bitmap bitmap, SelfDisposingBitmapDrawable baseDrawable) : base(res, bitmap)
        {
            _baseDrawable = new WeakReference<ISelfDisposingBitmapDrawable>(baseDrawable);
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

        public FFBitmapDrawable(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }

        public void SetPlaceholder(SelfDisposingBitmapDrawable drawable, int animationDuration)
        {
            if (!IsFadeAnimationRunning)
            {
                _alpha = 255;
                _fadeDuration = animationDuration;
                _startTimeMillis = SystemClock.UptimeMillis();
                _placeholder = drawable?.GetConstantState().NewDrawable() as BitmapDrawable;
				IsFadeAnimationRunning = true;
            }
        }

        public override void SetIsDisplayed(bool isDisplayed)
        {
            base.SetIsDisplayed(isDisplayed);

            if (_baseDrawable != null)
            {
                if (_baseDrawable.TryGetTarget(out var sdbDraw) && sdbDraw.IsValidAndHasValidBitmap())
                {
                    sdbDraw.SetIsDisplayed(isDisplayed);
                }
            }
        }

        public bool IsFadeAnimationRunning { get; private set; }
        public int FadeDuration => (int)_fadeDuration;

        protected override void OnBoundsChange(Rect bounds)
        {
            _orgRect = bounds;
            _placeholderInitialized = false;
            base.OnBoundsChange(bounds);
        }

        public override void Draw(Canvas canvas)
        {
            try
            {
                if (!IsFadeAnimationRunning)
                {
                    base.Draw(canvas);
                }
                else
                {
                    var normalized = (SystemClock.UptimeMillis() - _startTimeMillis) / _fadeDuration;
                    if (normalized >= 1f)
                    {
						IsFadeAnimationRunning = false;
                        _placeholder = null;
                        normalized = 1f;
                        base.Draw(canvas);
                    }
                    else
                    {
                        if (_placeholder.IsValidAndHasValidBitmap())
                        {
                            if (!_placeholderInitialized)
                            {
                                var placeholderSizeRatio = canvas.Width > canvas.Height ?
                                                                 (double)_orgRect.Right / _placeholder.IntrinsicWidth
                                                                 : (double)_orgRect.Bottom / _placeholder.IntrinsicHeight;

                                var scaledWidth = placeholderSizeRatio * _placeholder.IntrinsicWidth;
                                var newOffset = (double)_orgRect.CenterX() - scaledWidth / 2;
                                _placeholder.Gravity = Android.Views.GravityFlags.Fill;
                                _placeholder.SetBounds((int)newOffset, _orgRect.Top, _orgRect.Right, _orgRect.Bottom);

                                _placeholderInitialized = true;
                            }

                            _placeholder.Draw(canvas);
                        }

                        int partialAlpha = (int)(_alpha * normalized);
                        base.SetAlpha(partialAlpha);
                        base.Draw(canvas);
                        base.SetAlpha(_alpha);
                    }
                }
            }
            catch { }
        }

        public override void SetAlpha(int alpha)
        {
            try
            {
                if (_placeholder.IsValidAndHasValidBitmap())
                {
                    _placeholder.SetAlpha(alpha);
                }

                base.SetAlpha(alpha);
            }
            catch { }
        }

        public override void SetColorFilter(Color color, PorterDuff.Mode mode)
        {
            try
            {
                if (_placeholder.IsValidAndHasValidBitmap())
                {
                    _placeholder.SetColorFilter(color, mode);
                }

                base.SetColorFilter(color, mode);
            }
            catch { }
        }
    }
}
