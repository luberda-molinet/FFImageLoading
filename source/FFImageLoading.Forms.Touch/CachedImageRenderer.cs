using CoreGraphics;
using CoreAnimation;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using FFImageLoading.Work;
using Foundation;
using FFImageLoading.Forms;
using FFImageLoading.Extensions;
using System.Threading.Tasks;
using FFImageLoading.Helpers;
using FFImageLoading.Forms.Args;
using FFImageLoading.Forms.Platform;
using System.Reflection;

#if __IOS__
using UIKit;
using PImage = UIKit.UIImage;
using PImageView = UIKit.UIImageView;
using Xamarin.Forms.Platform.iOS;

#elif __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
using PImageView = FFImageLoading.Forms.Platform.CachedImageRenderer.FormsNSImageView;
using Xamarin.Forms.Platform.MacOS;
using System.IO;
#endif

namespace FFImageLoading.Forms.Platform
{
    /// <summary>
    /// CachedImage Implementation
    /// </summary>
    [Preserve(AllMembers = true)]
    public class CachedImageRenderer : ViewRenderer<CachedImage, PImageView>
    {
        [RenderWith(typeof(CachedImageRenderer))]
        internal class _CachedImageRenderer
        {
        }
        
        private bool _isDisposed;
		private IScheduledWork _currentTask;
        private ImageSourceBinding _lastImageSource;
        private readonly object _updateBitmapLock = new object();

        /// <summary>
        ///   Used for registration with dependency service
        /// </summary>
        public static new void Init()
        {
            CachedImage.IsRendererInitialized = true;

            // needed because of this STUPID linker issue: https://bugzilla.xamarin.com/show_bug.cgi?id=31076
#pragma warning disable 0219
            var ignore1 = typeof(CachedImageRenderer);
            var ignore2 = typeof(CachedImage);
#pragma warning restore 0219
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            ScaleHelper.InitAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

		/// <summary>
		/// Call this after Xamarin.Forms.Init to use FFImageLoading in all Xamarin.Forms views,
		/// including Xamarin.Forms.Image
		/// </summary>
		public static void InitImageSourceHandler()
		{
			Helpers.Dependency.Register(typeof(FileImageSource), typeof(FFImageLoadingImageSourceHandler));
			Helpers.Dependency.Register(typeof(StreamImageSource), typeof(FFImageLoadingImageSourceHandler));
			Helpers.Dependency.Register(typeof(UriImageSource), typeof(FFImageLoadingImageSourceHandler));
			Helpers.Dependency.Register(typeof(EmbeddedResourceImageSource), typeof(FFImageLoadingImageSourceHandler));
			Helpers.Dependency.Register(typeof(DataUrlImageSource), typeof(FFImageLoadingImageSourceHandler));

			try
			{
				var svgAssembly = Assembly.Load("FFImageLoading.Svg.Forms");
				if (svgAssembly != null)
				{
					var svgImageSourceType = svgAssembly.GetType("FFImageLoading.Svg.Forms.SvgImageSource");
					if (svgImageSourceType != null)
					{
						Helpers.Dependency.Register(svgImageSourceType, typeof(FFImageLoadingImageSourceHandler));
					}
				}
			}
			catch { }
		}

		protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                CancelIfNeeded();
            }

            base.Dispose(disposing);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<CachedImage> e)
        {
            base.OnElementChanged(e);

            if (Control == null && Element != null && !_isDisposed)
            {
#if __IOS__
                SetNativeControl(new PImageView(CGRect.Empty)
                {
                    ContentMode = UIViewContentMode.ScaleAspectFit,
                    ClipsToBounds = true
                });
#elif __MACOS__
                SetNativeControl(new PImageView());
#endif
            }

            if (e.OldElement != null)
            {
                e.OldElement.InternalReloadImage = null;
                e.OldElement.InternalCancel = null;
                e.OldElement.InternalGetImageAsJPG = null;
                e.OldElement.InternalGetImageAsPNG = null;
            }

            if (e.NewElement != null)
            {
				e.NewElement.InternalReloadImage = new Action(ReloadImage);
                e.NewElement.InternalCancel = new Action(CancelIfNeeded);
                e.NewElement.InternalGetImageAsJPG = new Func<GetImageAsJpgArgs, Task<byte[]>>(GetImageAsJpgAsync);
                e.NewElement.InternalGetImageAsPNG = new Func<GetImageAsPngArgs, Task<byte[]>>(GetImageAsPngAsync);

				SetOpacity();
				SetAspect();
                UpdateImage(Control, Element, e.OldElement);
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == CachedImage.SourceProperty.PropertyName)
            {
                UpdateImage(Control, Element, null);
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
            if (Control == null || Control.Handle == IntPtr.Zero || Element == null || _isDisposed)
                return;
#if __IOS__
            Control.ContentMode = Element.Aspect.ToUIViewContentMode();
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
            if (Control == null || Control.Handle == IntPtr.Zero || Element == null || _isDisposed)
                return;
#if __IOS__
            Control.Opaque = Element.IsOpaque;
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
                        _currentTask = imageLoader.Into(imageView);
                }
            }
        }

		private async void ImageLoadingSizeChanged(CachedImage element, bool isLoading)
		{
			if (element == null || _isDisposed)
				return;

			await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() =>
			{
				if (element == null || _isDisposed)
					return;

				((IVisualElementController)element).NativeSizeChanged();

				if (!isLoading)
					element.SetIsLoading(isLoading);
			}).ConfigureAwait(false);
		}

		private void ReloadImage()
        {
            UpdateImage(Control, Element, null);
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

            await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() =>
            {
                if (Control != null)
                    image = Control.Image;
            }).ConfigureAwait(false);

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

