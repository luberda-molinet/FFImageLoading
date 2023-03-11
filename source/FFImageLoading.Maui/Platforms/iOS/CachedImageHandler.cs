using CoreGraphics;
using CoreAnimation;
using System;
using System.ComponentModel;
using FFImageLoading.Work;
using Foundation;
using FFImageLoading.Maui;
using FFImageLoading.Extensions;
using System.Threading.Tasks;
using FFImageLoading.Helpers;
using FFImageLoading.Maui.Args;
using FFImageLoading.Maui.Platform;
using System.Reflection;
using Microsoft.Maui.Handlers;

#if __IOS__
using UIKit;
using PImage = UIKit.UIImage;
using PImageView = UIKit.UIImageView;

#elif __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
using PImageView = FFImageLoading.Maui.Platform.CachedImageRenderer.FormsNSImageView;
using Xamarin.Maui.Platform.MacOS;
using System.IO;
#endif

namespace FFImageLoading.Maui.Platform
{
    /// <summary>
    /// CachedImage Implementation
    /// </summary>
    [Preserve(AllMembers = true)]
    public class CachedImageHandler : ViewHandler<CachedImage, PImageView>
    {
        private bool _isDisposed;
		private IScheduledWork _currentTask;
        private ImageSourceBinding _lastImageSource;
        private readonly object _updateBitmapLock = new object();

		public CachedImageHandler() : base(ViewHandler.ViewMapper, ViewHandler.ViewCommandMapper)
		{
		}

		public CachedImageHandler(IPropertyMapper mapper, CommandMapper commandMapper = null) : base(mapper, commandMapper)
		{
		}

		IImageService<TImageContainer> ImageService
			=> this.VirtualView.FindMauiContext()?.Services.GetRequiredService<IImageService<TImageContainer>>();

		protected override PImageView CreatePlatformView()
		{
			return new PImageView(CGRect.Empty)
			{
				ContentMode = UIViewContentMode.ScaleAspectFit,
				ClipsToBounds = true
			};
		}

		protected override void ConnectHandler(PImageView platformView)
		{
			VirtualView.InternalReloadImage = new Action(ReloadImage);
			VirtualView.InternalCancel = new Action(CancelIfNeeded);
			VirtualView.InternalGetImageAsJPG = new Func<GetImageAsJpgArgs, Task<byte[]>>(GetImageAsJpgAsync);
			VirtualView.InternalGetImageAsPNG = new Func<GetImageAsPngArgs, Task<byte[]>>(GetImageAsPngAsync);

			VirtualView.PropertyChanged += VirtualView_PropertyChanged;

			SetOpacity();
			SetAspect();
			UpdateImage(platformView, VirtualView, null);

			base.ConnectHandler(platformView);
		}

		protected override void DisconnectHandler(PImageView platformView)
		{
			VirtualView.PropertyChanged -= VirtualView_PropertyChanged;

			VirtualView.InternalReloadImage = null;
			VirtualView.InternalCancel = null;
			VirtualView.InternalGetImageAsJPG = null;
			VirtualView.InternalGetImageAsPNG = null;

			CancelIfNeeded();

			base.DisconnectHandler(platformView);
		}

		void VirtualView_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == CachedImage.SourceProperty.PropertyName)
            {
                UpdateImage(PlatformView, VirtualView, null);
            }
            if (e.PropertyName == CachedImage.IsOpaqueProperty.PropertyName)
            {
                SetOpacity();
            }
            if (e.PropertyName == CachedImage.AspectProperty.PropertyName)
            {
                SetAspect();
            }
        }

        private void SetAspect()
        {
            if (PlatformView == null || PlatformView.Handle == IntPtr.Zero || VirtualView == null || _isDisposed)
                return;
#if __IOS__
			PlatformView.ContentMode = VirtualView.Aspect switch
			{
				Aspect.AspectFit => UIViewContentMode.ScaleAspectFit,
				Aspect.AspectFill => UIViewContentMode.ScaleAspectFill,
				Aspect.Fill => UIViewContentMode.ScaleToFill,
				Aspect.Center => UIViewContentMode.Center,
				_ => UIViewContentMode.ScaleAspectFit,
			};
#elif __MACOS__
            switch (Element.Aspect)
            {
                case Aspect.AspectFill:
                    Control.Layer.ContentsGravity = CALayer.GravityResizeAspectFill;
                    break;
                case Aspect.Fill:
                    Control.Layer.ContentsGravity = CALayer.GravityResize;
                    break;
                case Aspect.AspectFit:
                default:
                    Control.Layer.ContentsGravity = CALayer.GravityResizeAspect;
                    break;
            }
#endif
		}

        private void SetOpacity()
        {
            if (PlatformView == null || PlatformView.Handle == IntPtr.Zero || VirtualView == null || _isDisposed)
                return;
#if __IOS__
			PlatformView.Opaque = VirtualView.IsOpaque;
#elif __MACOS__
            Control.SetIsOpaque(Element.IsOpaque);
#endif
        }

        private void UpdateImage(PImageView imageView, CachedImage image, CachedImage previousImage)
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
                    imageView.Image = null;
                    return;
                }

                if (previousImage != null && !ffSource.Equals(_lastImageSource))
                {
                    _lastImageSource = null;
                    imageView.Image = null;
                }

                image.SetIsLoading(true);

                var placeholderSource = ImageSourceBinding.GetImageSourceBinding(image.LoadingPlaceholder, image);
                var errorPlaceholderSource = ImageSourceBinding.GetImageSourceBinding(image.ErrorPlaceholder, image);
                image.SetupOnBeforeImageLoading(out var imageLoader, ffSource, placeholderSource, errorPlaceholderSource);

				// Try and get density from the view to ensure it comes from the appropriate display
				// the view is on, but fallback to a main display value
				imageLoader.Scale = VirtualView?.GetVisualElementWindow()?.RequestDisplayDensity()
					?? (float)DeviceDisplay.MainDisplayInfo.Density;

				if (imageLoader != null)
                {
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

			if (element.Dispatcher is not null)
			{
				await element.Dispatcher.DispatchAsync(() =>
				{
					if (element == null || _isDisposed)
						return;

					((IVisualElementController)element).InvalidateMeasure(Microsoft.Maui.Controls.Internals
						.InvalidationTrigger.MeasureChanged);

					if (!isLoading)
						element.SetIsLoading(isLoading);
				}).ConfigureAwait(false);
			}
		}

		private void ReloadImage()
        {
            UpdateImage(PlatformView, VirtualView, null);
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
            return GetImageAsByteAsync(false, args.Quality, args.DesiredWidth, args.DesiredHeight);
        }

        private Task<byte[]> GetImageAsPngAsync(GetImageAsPngArgs args)
        {
            return GetImageAsByteAsync(true, 90, args.DesiredWidth, args.DesiredHeight);
        }

        private async Task<byte[]> GetImageAsByteAsync(bool usePNG, int quality, int desiredWidth, int desiredHeight)
        {
            PImage image = null;

            if (PlatformView is not null && VirtualView.Dispatcher is not null)
            {
	            await VirtualView.Dispatcher.DispatchAsync(() =>
	            {
		            if (PlatformView != null)
			            image = PlatformView.Image;
	            }).ConfigureAwait(false);
            }

            if (image == null)
                return null;

            if (desiredWidth != 0 || desiredHeight != 0)
            {
                image = image.ResizeUIImage(desiredWidth, desiredHeight, InterpolationMode.Default);
            }

#if __IOS__

            var imageData = usePNG ? image.AsPNG() : image.AsJPEG((nfloat)quality / 100f);

            if (imageData == null || imageData.Length == 0)
                return null;

            var encoded = imageData.ToArray();
            imageData.TryDispose();
            return encoded;
#elif __MACOS__

            byte[] encoded;
            using (MemoryStream ms = new MemoryStream())
            using (var stream = usePNG ? image.AsPngStream() : image.AsJpegStream(quality))
            {
                stream.CopyTo(ms);
                encoded = ms.ToArray();
            }

            if (desiredWidth != 0 || desiredHeight != 0)
            {
                image.TryDispose();
            }

            return encoded;
#endif
        }

#if __MACOS__
        public class FormsNSImageView : NSImageView
        {
            bool _isOpaque;

            public FormsNSImageView()
            {
                //Layer = new FFCALayer();
                //WantsLayer = true;
            }

            public void SetIsOpaque(bool isOpaque)
            {
                _isOpaque = isOpaque;
            }

            //public override void DrawRect(CGRect dirtyRect)
            //{
            //    // TODO if it isn't disabled then this issue happens:
            //    // https://github.com/luberda-molinet/FFImageLoading/issues/922
            //    // base.DrawRect(dirtyRect);
            //}

            public override bool IsOpaque => _isOpaque;
        }
#endif
    }
}

