using Android.Content;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Widget;
using System;
using Android.Runtime;
using FFImageLoading.Drawables;
using FFImageLoading.Work;
using System.Threading.Tasks;
using FFImageLoading.Helpers;
using System.Threading;

namespace FFImageLoading.Views
{
    public class ManagedImageView : ImageView
    {
        WeakReference<Drawable> _drawableRef;
        CancellationTokenSource _tcs;
        readonly object _lock = new object();

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
            if (disposing)
            {
                try
                {
                    CancelGifPlay();
                    _tcs?.TryDispose();
                }
                catch (Exception) { }

                if (_drawableRef != null)
                {
                    Drawable drawable = null;

                    if (_drawableRef.TryGetTarget(out drawable))
                    {
                        UpdateDrawableDisplayedState(drawable, false);
                    }

                    _drawableRef = null;
                }
            }

            base.Dispose(disposing);
        }

        void CancelGifPlay()
        {
            try
            {
                _tcs?.Cancel();
            }
            catch (ObjectDisposedException) { }
        }

        void PlayGif(FFGifDrawable gifDrawable, CancellationTokenSource tokenSource)
        {
            var token = tokenSource.Token;

            Task.Run(async () =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    var gifDecoder = gifDrawable.GifHelper;
                    int n = gifDecoder.GetFrameCount();
                    //int ntimes = gifDecoder.GetLoopCount();
                    //TODO DISABLED for endless loop
                    int ntimes = 0;
                    int repetitionCounter = 0;

                    do
                    {
                        for (int i = 0; i < n; i++)
                        {
                            token.ThrowIfCancellationRequested();
                            var bitmap = gifDecoder.GetFrame(i);
                            int t = gifDecoder.GetDelay(i);

                            if (bitmap != null && bitmap.Handle != IntPtr.Zero && !bitmap.IsRecycled)
                            {
                                await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() => base.SetImageBitmap(bitmap)).ConfigureAwait(false);
                            }

                            token.ThrowIfCancellationRequested();
                            await Task.Delay(t, token).ConfigureAwait(false);
                        }
                        if (ntimes != 0)
                        {
                            repetitionCounter++;
                        }
                    } while (repetitionCounter <= ntimes);
                }
                catch (Exception)
                {
                }
                finally
                {
                    tokenSource.TryDispose();
                }
            }, token);
        }

        /* FMT: this is not fine when working with RecyclerView... It can detach and cache the view, then reattach it
        protected override void OnDetachedFromWindow()
        {
            SetImageDrawable(null);
            base.OnDetachedFromWindow();
        }
        */

        public IImageLoaderTask ImageLoaderTask { get; set; }

        public void CancelLoading()
        {
            if (ImageLoaderTask != null)
            {
                ImageService.Instance.CancelWorkFor(ImageLoaderTask);
                ImageLoaderTask = null;
            }
        }

        public override void SetImageDrawable(Drawable drawable)
        {
            lock (_lock)
            {
                var previous = Drawable;

                var gifDrawable = drawable as FFGifDrawable;
                if (gifDrawable != null)
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

        void UpdateDrawableDisplayedState(Drawable drawable, bool isDisplayed)
        {
            if (drawable == null || drawable.Handle == IntPtr.Zero)
                return;

            var selfDisposingBitmapDrawable = drawable as ISelfDisposingBitmapDrawable;
            if (selfDisposingBitmapDrawable != null)
            {
                if (selfDisposingBitmapDrawable.HasValidBitmap)
                    selfDisposingBitmapDrawable.SetIsDisplayed(isDisplayed);
            }
            else
            {
                var layerDrawable = drawable as LayerDrawable;
                if (layerDrawable != null)
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
