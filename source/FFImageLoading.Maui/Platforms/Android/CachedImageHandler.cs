using Android.Widget;
using System;
using System.ComponentModel;
using FFImageLoading.Work;
using FFImageLoading.Maui.Platform;
using FFImageLoading.Maui;
using Android.Runtime;
using Android.Graphics.Drawables;
using Android.Graphics;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Maui.Args;
using FFImageLoading.Helpers;
using FFImageLoading.Views;
using Android.Views;
using System.Reflection;
using Android.Content;
using Microsoft.Maui.Handlers;

namespace FFImageLoading.Maui.Platform
{
    /// <summary>
    /// CachedImage Implementation
    /// </summary>
    [Preserve(AllMembers=true)]
    public class CachedImageHandler : ViewHandler<CachedImage, CachedImageView>
    {

		private bool _isDisposed;
		private IScheduledWork _currentTask;
        private ImageSourceBinding _lastImageSource;

        //private readonly static Type _platformDefaultRendererType = typeof(ImageRenderer).Assembly.GetType("Xamarin.Forms.Platform.Android.Platform+DefaultRenderer");
        //private static readonly MethodInfo _platformDefaultRendererTypeNotifyFakeHandling = _platformDefaultRendererType?.GetMethod("NotifyFakeHandling", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private readonly object _updateBitmapLock = new object();

		public CachedImageHandler() : base(ViewHandler.ViewMapper, ViewHandler.ViewCommandMapper)
		{
		}

		public CachedImageHandler(IPropertyMapper mapper, CommandMapper commandMapper = null) : base(mapper, commandMapper)
		{
		}

		IImageService<TImageContainer> ImageService
			=> MauiContext.Services.GetRequiredService<IImageService<TImageContainer>>();

		protected override CachedImageView CreatePlatformView()
		{
			return new CachedImageView(Context);
		}


		protected override void ConnectHandler(CachedImageView platformView)
		{
			VirtualView.InternalReloadImage = new Action(ReloadImage);
			VirtualView.InternalCancel = new Action(CancelIfNeeded);
			VirtualView.InternalGetImageAsJPG = new Func<GetImageAsJpgArgs, Task<byte[]>>(GetImageAsJpgAsync);
			VirtualView.InternalGetImageAsPNG = new Func<GetImageAsPngArgs, Task<byte[]>>(GetImageAsPngAsync);

			VirtualView.PropertyChanged += VirtualView_PropertyChanged;

			//_motionEventHelper?.UpdateElement(VirtualView);
			UpdateBitmap(platformView, VirtualView, null);
			UpdateAspect();

			base.ConnectHandler(platformView);
		}

		protected override void DisconnectHandler(CachedImageView platformView)
		{
			VirtualView.PropertyChanged -= VirtualView_PropertyChanged;

			VirtualView.InternalReloadImage = null;
			VirtualView.InternalCancel = null;
			VirtualView.InternalGetImageAsJPG = null;
			VirtualView.InternalGetImageAsPNG = null;

			CancelIfNeeded();

			base.DisconnectHandler(platformView);
		}

		private void VirtualView_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == CachedImage.SourceProperty.PropertyName)
			{
				UpdateBitmap(PlatformView, VirtualView, null);
			}
			if (e.PropertyName == CachedImage.AspectProperty.PropertyName)
			{
				UpdateAspect();
			}
		}


		private void UpdateAspect()
        {
            if (PlatformView == null || PlatformView.Handle == IntPtr.Zero || VirtualView == null || _isDisposed)
                return;

            if (VirtualView.Aspect == Aspect.AspectFill)
				PlatformView.SetScaleType(ImageView.ScaleType.CenterCrop);

            else if (VirtualView.Aspect == Aspect.Fill)
				PlatformView.SetScaleType(ImageView.ScaleType.FitXy);

            else
				PlatformView.SetScaleType(ImageView.ScaleType.FitCenter);
        }

        private void UpdateBitmap(CachedImageView imageView, CachedImage image, CachedImage previousImage)
        {
            lock (_updateBitmapLock)
            {
                CancelIfNeeded();

                if (image == null || imageView == null || imageView.Handle == IntPtr.Zero || _isDisposed)
                    return;

                var ffSource = ImageSourceBinding.GetImageSourceBinding(image.Source, image);
                if (ffSource == null)
                {
                    if (_lastImageSource == null)
                        return;

                    _lastImageSource = null;
                    imageView.SetImageResource(global::Android.Resource.Color.Transparent);
                    return;
                }

                if (previousImage != null && !ffSource.Equals(_lastImageSource))
                {
                    _lastImageSource = null;
                    imageView.SkipInvalidate();
					PlatformView.SetImageResource(global::Android.Resource.Color.Transparent);
                }

                image.SetIsLoading(true);

                var placeholderSource = ImageSourceBinding.GetImageSourceBinding(image.LoadingPlaceholder, image);
                var errorPlaceholderSource = ImageSourceBinding.GetImageSourceBinding(image.ErrorPlaceholder, image);
                image.SetupOnBeforeImageLoading(out var imageLoader, ffSource, placeholderSource, errorPlaceholderSource);

				if (imageLoader != null)
                {
					// Try and get density from the view to ensure it comes from the appropriate display
					// the view is on, but fallback to a main display value
					imageLoader.Scale = VirtualView?.GetVisualElementWindow()?.RequestDisplayDensity()
						?? (float)DeviceDisplay.MainDisplayInfo.Density;

					var finishAction = imageLoader.OnFinish;
                    var sucessAction = imageLoader.OnSuccess;

                    imageLoader.Finish((work) =>
                    {
                        finishAction?.Invoke(work);
                        ImageLoadingSizeChanged(image, false);
                    });

                    imageLoader.Success((imageInformation, loadingResult) =>
                    {
                        sucessAction?.Invoke(imageInformation, loadingResult);
                        _lastImageSource = ffSource;
                    });

                    imageLoader.LoadingPlaceholderSet(() => ImageLoadingSizeChanged(image, true));

                    if (!_isDisposed)
                        _currentTask = imageLoader.Into(imageView, ImageService);
                }
            }
        }

		private async void ImageLoadingSizeChanged(CachedImage element, bool isLoading)
		{
			if (element == null || _isDisposed)
				return;

			await ImageService.Dispatcher.PostAsync(() =>
			{
				if (element == null || _isDisposed)
					return;

				((IVisualElementController)element).InvalidateMeasure(Microsoft.Maui.Controls.Internals.InvalidationTrigger.MeasureChanged);

				if (!isLoading)
					element.SetIsLoading(isLoading);
			}).ConfigureAwait(false);
		}

		private void ReloadImage()
        {
            UpdateBitmap(PlatformView, VirtualView, null);
        }

        private void CancelIfNeeded()
        {
            try
            {
                _currentTask?.Cancel();
            }
            catch { }
        }

        private Task<byte[]> GetImageAsJpgAsync(GetImageAsJpgArgs args)
        {
            return GetImageAsByteAsync(Bitmap.CompressFormat.Jpeg, args.Quality, args.DesiredWidth, args.DesiredHeight);
        }

        private Task<byte[]> GetImageAsPngAsync(GetImageAsPngArgs args)
        {
            return GetImageAsByteAsync(Bitmap.CompressFormat.Png, 90, args.DesiredWidth, args.DesiredHeight);
        }

        private async Task<byte[]> GetImageAsByteAsync(Bitmap.CompressFormat format, int quality, int desiredWidth, int desiredHeight)
        {
            if (PlatformView == null)
                return null;

            if (!(PlatformView.Drawable is BitmapDrawable drawable) || drawable.Bitmap == null)
                return null;

            var bitmap = drawable.Bitmap;

            if (desiredWidth != 0 || desiredHeight != 0)
            {
                var widthRatio = (double)desiredWidth / bitmap.Width;
                var heightRatio = (double)desiredHeight / bitmap.Height;
                var scaleRatio = Math.Min(widthRatio, heightRatio);

                if (desiredWidth == 0)
                    scaleRatio = heightRatio;

                if (desiredHeight == 0)
                    scaleRatio = widthRatio;

                var aspectWidth = (int)(bitmap.Width * scaleRatio);
                var aspectHeight = (int)(bitmap.Height * scaleRatio);

                bitmap = Bitmap.CreateScaledBitmap(bitmap, aspectWidth, aspectHeight, true);
            }

            using (var stream = new MemoryStream())
            {
                await bitmap.CompressAsync(format, quality, stream).ConfigureAwait(false);
                var compressed = stream.ToArray();

                if (desiredWidth != 0 || desiredHeight != 0)
                {
                    bitmap.Recycle();
                    bitmap.TryDispose();
                }

                return compressed;
            }
        }
    }
}

