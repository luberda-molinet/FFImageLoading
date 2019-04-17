using Android.Content;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;
using System;
using Android.Runtime;
using FFImageLoading.Drawables;
using System.Threading;

namespace FFImageLoading.Views
{
    public class ManagedImageView : ImageView
    {
        private WeakReference<Drawable> _drawableRef;
        private CancellationTokenSource _tcs;
        private readonly object _lock = new object();
        private HighResolutionTimer<Android.Graphics.Bitmap> _animationTimer;
        private bool _isDisposed;
        private bool _animationFrameSetting;

        public ManagedImageView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public ManagedImageView(Context context) : base(context)
        {
        }

        public ManagedImageView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public ManagedImageView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
        }

        protected override void Dispose(bool disposing)
        {
            _isDisposed = true;

            if (disposing)
            {
                try
                {
                    CancelGifPlay();
                    _tcs?.TryDispose();
                }
                catch { }

                if (_drawableRef != null)
                {
                    if (_drawableRef.TryGetTarget(out var drawable))
                    {
                        UpdateDrawableDisplayedState(drawable, false);
                    }

                    _drawableRef = null;
                }
            }

            base.Dispose(disposing);
        }

        private void CancelGifPlay()
        {
            try
            {
                _animationTimer?.Stop();
                _tcs?.Cancel();
            }
            catch (ObjectDisposedException) { }
        }

        private void PlayGif(FFGifDrawable gifDrawable, CancellationTokenSource tokenSource)
        {
            var token = tokenSource.Token;
            var animatedImages = gifDrawable.AnimatedImages;

            _animationTimer?.Stop();
            _animationTimer = new HighResolutionTimer<Android.Graphics.Bitmap>(gifDrawable.AnimatedImages, async (image) =>
            {
                if (_animationFrameSetting)
                    return;

                _animationFrameSetting = true;

                try
                {
                    var bitmap = image.Image;

                    if (_isDisposed || !_animationTimer.Enabled)
                        return;

                    if (bitmap != null && bitmap.Handle != IntPtr.Zero && !bitmap.IsRecycled)
                    {
                        if (_isDisposed || !_animationTimer.Enabled)
                            return;

                        await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() =>
                        {
                            if (_isDisposed || !_animationTimer.Enabled)
                                return;

                            base.SetImageBitmap(bitmap);
                            ;
                        }).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    ImageService.Instance.Config.Logger.Error("GIF", ex);
                }
                finally
                {
                    _animationFrameSetting = false;
                }
            })
            {
                DelayOffset = -2
            };
            _animationTimer.Start();
        }

        public void CancelLoading()
        {
            ImageService.Instance.CancelWorkForView(this);
        }

        public override void SetImageDrawable(Drawable drawable)
        {
            lock (_lock)
            {
                var previous = Drawable;

                if (drawable is FFGifDrawable gifDrawable)
                {
                    CancelGifPlay();
                    var oldTcs = _tcs;
                    _tcs = new CancellationTokenSource();
                    oldTcs.TryDispose();

                    _drawableRef = new WeakReference<Drawable>(drawable);
                    UpdateDrawableDisplayedState(drawable, true);
                    UpdateDrawableDisplayedState(previous, false);

                    PlayGif(gifDrawable, _tcs);
                }
                else
                {
                    if (drawable == null || drawable is ISelfDisposingBitmapDrawable)
                    {
                        CancelGifPlay();
                    }

                    _drawableRef = new WeakReference<Drawable>(drawable);
                    base.SetImageDrawable(drawable);
                    UpdateDrawableDisplayedState(drawable, true);
                    UpdateDrawableDisplayedState(previous, false);
                }
            }
        }

        public override void SetImageResource(int resId)
        {
            CancelGifPlay();

            var previous = Drawable;
            // Ultimately calls SetImageDrawable, where the state will be updated.
            _drawableRef = null;
            base.SetImageResource(resId);
            UpdateDrawableDisplayedState(previous, false);
        }

        public override void SetImageURI(global::Android.Net.Uri uri)
        {
            CancelGifPlay();

            var previous = Drawable;
            // Ultimately calls SetImageDrawable, where the state will be updated.
            _drawableRef = null;
            base.SetImageURI(uri);
            UpdateDrawableDisplayedState(previous, false);
        }

        private void UpdateDrawableDisplayedState(Drawable drawable, bool isDisplayed)
        {
            if (drawable == null || drawable.Handle == IntPtr.Zero)
                return;

            if (drawable is ISelfDisposingBitmapDrawable selfDisposingBitmapDrawable)
            {
                if (selfDisposingBitmapDrawable.HasValidBitmap)
                    selfDisposingBitmapDrawable.SetIsDisplayed(isDisplayed);
            }
            else
            {
                if (drawable is LayerDrawable layerDrawable)
                {
                    for (var i = 0; i < layerDrawable.NumberOfLayers; i++)
                    {
                        UpdateDrawableDisplayedState(layerDrawable.GetDrawable(i), isDisplayed);
                    }
                }
            }
        }
    }

}
