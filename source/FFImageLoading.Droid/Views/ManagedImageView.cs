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
		private WeakReference<Drawable> _drawableRef = null;
        CancellationTokenSource _tcs;

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
            _tcs?.Cancel();

			if (_drawableRef != null)
			{
				Drawable drawable = null; 

				if (_drawableRef.TryGetTarget(out drawable))
				{
					UpdateDrawableDisplayedState(drawable, false);
				}	

				_drawableRef = null;
			}

			base.Dispose(disposing);
		}

        async void PlayGif(FFGifDrawable gifDrawable, CancellationToken token)
        {
            try
            {
                var gifDecoder = gifDrawable.GifDecoder;
                int n = gifDecoder.GetFrameCount();
                //int ntimes = gifDecoder.GetLoopCount();
                //TODO DISABLED for endless loop
                int ntimes = 0;
                int repetitionCounter = 0;

                do
                {
                    token.ThrowIfCancellationRequested();

                    for (int i = 0; i < n; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        var bitmap = gifDecoder.GetFrame(i);
                        int t = gifDecoder.GetDelay(i);
                        token.ThrowIfCancellationRequested();

                        if (bitmap != null && bitmap.Handle != IntPtr.Zero && !bitmap.IsRecycled)
                            SetImageBitmap(bitmap);
                        
                        token.ThrowIfCancellationRequested();
                        await Task.Delay(t);
                    }
                    if (ntimes != 0)
                    {
                        repetitionCounter++;
                    }
                }
                while (repetitionCounter <= ntimes);
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
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
            var gifDrawable = drawable as FFGifDrawable;
            if (gifDrawable != null)
            {
                _tcs?.Cancel();
                _tcs = new CancellationTokenSource();

                var previous = Drawable;
                _drawableRef = new WeakReference<Drawable>(drawable);
                base.SetImageDrawable(drawable);
                UpdateDrawableDisplayedState(drawable, true);
                UpdateDrawableDisplayedState(previous, false);

                PlayGif(gifDrawable, _tcs.Token);

                return;
            }

            GifDecoder currenGifDecoder = null;
            Drawable currentDrawable = null;
            if (_drawableRef != null && _drawableRef.TryGetTarget(out currentDrawable) && currentDrawable != null)
            {
                var currentGifDrawable = currentDrawable as FFGifDrawable;
                currenGifDecoder = currentGifDrawable?.GifDecoder;
            }

            var bitmapDrawable = drawable as BitmapDrawable;
            if (bitmapDrawable == null || currenGifDecoder == null || !currenGifDecoder.ContainsBitmap(bitmapDrawable?.Bitmap))
            {
                _tcs?.Cancel();
                var previous = Drawable;
                _drawableRef = new WeakReference<Drawable>(drawable);
                base.SetImageDrawable(drawable);
                UpdateDrawableDisplayedState(drawable, true);
                UpdateDrawableDisplayedState(previous, false);
            }
            else
            {
                base.SetImageDrawable(drawable);
            }
		}

		public override void SetImageResource(int resId)
		{
            _tcs?.Cancel();

            var previous = Drawable;
            // Ultimately calls SetImageDrawable, where the state will be updated.
            _drawableRef = null;
            base.SetImageResource(resId);
            UpdateDrawableDisplayedState(previous, false);
		}

		public override void SetImageURI(global::Android.Net.Uri uri)
		{
            _tcs?.Cancel();

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