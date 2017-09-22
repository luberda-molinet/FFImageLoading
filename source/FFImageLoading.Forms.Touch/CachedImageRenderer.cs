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

#if __IOS__
using UIKit;
using PImage = UIKit.UIImage;
using PImageView = UIKit.UIImageView;
using Xamarin.Forms.Platform.iOS;
using FFImageLoading.Forms.Touch;
#elif __MACOS__
using AppKit;
using PImage = AppKit.NSImage;
using PImageView = FFImageLoading.Forms.Mac.CachedImageRenderer.FormsNSImageView;
using Xamarin.Forms.Platform.MacOS;
using FFImageLoading.Forms.Mac;
using System.IO;
#endif

[assembly: ExportRenderer(typeof(CachedImage), typeof(CachedImageRenderer))]
#if __IOS__
namespace FFImageLoading.Forms.Touch
#elif __MACOS__
namespace FFImageLoading.Forms.Mac
#endif
{
    /// <summary>
    /// CachedImage Implementation
    /// </summary>
    [Preserve(AllMembers = true)]
    public class CachedImageRenderer : ViewRenderer<CachedImage, PImageView>
    {
        private bool _isDisposed;
        private IScheduledWork _currentTask;
        private ImageSourceBinding _lastImageSource;

        /// <summary>
        ///   Used for registration with dependency service
        /// </summary>
        public static new void Init()
        {
            // needed because of this STUPID linker issue: https://bugzilla.xamarin.com/show_bug.cgi?id=31076
#pragma warning disable 0219
            var dummy = new CachedImageRenderer();
#pragma warning restore 0219
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

                SetAspect();
                UpdateImage(Control, Element, e.OldElement);
                SetOpacity();
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

        void SetAspect()
        {
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

        void SetOpacity()
        {
#if __IOS__
            Control.Opaque = Element.IsOpaque;
#elif __MACOS__            
            Control.SetIsOpaque(Element.IsOpaque);
#endif
        }

        void UpdateImage(PImageView imageView, CachedImage image, CachedImage previousImage)
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
            TaskParameter imageLoader;
            image.SetupOnBeforeImageLoading(out imageLoader, ffSource, placeholderSource, errorPlaceholderSource);

            if (imageLoader != null)
            {
                var finishAction = imageLoader.OnFinish;
                var sucessAction = imageLoader.OnSuccess;

                imageLoader.Finish((work) =>
                {
                    finishAction?.Invoke(work);
                    ImageLoadingFinished(image);
                });

                imageLoader.Success((imageInformation, loadingResult) =>
                {
                    sucessAction?.Invoke(imageInformation, loadingResult);
                    _lastImageSource = ffSource;
                });

                _currentTask = imageLoader.Into(imageView);
            }
        }

        void ImageLoadingFinished(CachedImage element)
        {
            MainThreadDispatcher.Instance.Post(() =>
            {
                if (element != null && !_isDisposed)
                {
                    ((IVisualElementController)element).NativeSizeChanged();
                    element.SetIsLoading(false);
                }
            });
        }

        void ReloadImage()
        {
            UpdateImage(Control, Element, null);
        }

        void CancelIfNeeded()
        {
            try
            {
                var taskToCancel = _currentTask;
                if (taskToCancel != null && !taskToCancel.IsCancelled)
                {
                    taskToCancel.Cancel();
                }
            }
            catch (Exception) { }
        }

        Task<byte[]> GetImageAsJpgAsync(GetImageAsJpgArgs args)
        {
            return GetImageAsByteAsync(false, args.Quality, args.DesiredWidth, args.DesiredHeight);
        }

        Task<byte[]> GetImageAsPngAsync(GetImageAsPngArgs args)
        {
            return GetImageAsByteAsync(true, 90, args.DesiredWidth, args.DesiredHeight);
        }

        async Task<byte[]> GetImageAsByteAsync(bool usePNG, int quality, int desiredWidth, int desiredHeight)
        {
            PImage image = null;

            await MainThreadDispatcher.Instance.PostAsync(() =>
            {
                if (Control != null)
                    image = Control.Image;
            }).ConfigureAwait(false);

            if (image == null)
                return null;

            if (desiredWidth != 0 || desiredHeight != 0)
            {
                image = image.ResizeUIImage((double)desiredWidth, (double)desiredHeight, InterpolationMode.Default);
            }

#if __IOS__

            NSData imageData = usePNG ? image.AsPNG() : image.AsJPEG((nfloat)quality / 100f);

            if (imageData == null || imageData.Length == 0)
                return null;

            var encoded = imageData.ToArray();
            imageData.Dispose();
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
                image.Dispose();
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
                Layer = new CALayer();
                WantsLayer = true;
            }

            public void SetIsOpaque(bool isOpaque)
            {
                _isOpaque = isOpaque;
            }

            public override bool IsOpaque => _isOpaque;
        }
#endif
    }
}

