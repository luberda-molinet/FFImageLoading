using FFImageLoading.Forms;
using FFImageLoading.Forms.WinRT;
using FFImageLoading.Work;
using System;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Xamarin.Forms.Platform.WinRT;
using Xamarin.Forms;

[assembly: ExportRenderer(typeof(CachedImage), typeof(CachedImageRenderer))]
namespace FFImageLoading.Forms.WinRT
{
    /// <summary>
    /// CachedImage Implementation
    /// </summary>
    public class CachedImageRenderer : ViewRenderer<CachedImage, Windows.UI.Xaml.Controls.Image>
    {
        private IScheduledWork _currentTask;

        /// <summary>
        ///   Used for registration with dependency service
        /// </summary>
        public static void Init()
        {
            new ImageRenderer();

            CachedImage.CacheCleared += CachedImageCacheCleared;
            CachedImage.CacheInvalidated += CachedImageCacheInvalidated;
        }

        private static void CachedImageCacheInvalidated(object sender, CachedImageEvents.CacheInvalidatedEventArgs e)
        {
            ImageService.Invalidate(e.Key, e.CacheType);
        }

        private static void CachedImageCacheCleared(object sender, CachedImageEvents.CacheClearedEventArgs e)
        {
            switch (e.CacheType)
            {
                case Cache.CacheType.Memory:
                    ImageService.InvalidateMemoryCache();
                    break;
                case Cache.CacheType.Disk:
                    ImageService.InvalidateDiskCache();
                    break;
                case Cache.CacheType.All:
                    ImageService.InvalidateMemoryCache();
                    ImageService.InvalidateDiskCache();
                    break;
            }
        }

        private bool measured;

        public override SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            var bitmapSource = Control.Source as BitmapSource;

            if (bitmapSource == null)
                return new SizeRequest();

            measured = true;

            return new SizeRequest(new Size()
            {
                Width = bitmapSource.PixelWidth,
                Height = bitmapSource.PixelHeight
            });
        }

        protected override void OnElementChanged(ElementChangedEventArgs<CachedImage> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement == null)
                return;

            if (Control == null)
            {
                Windows.UI.Xaml.Controls.Image control = new Windows.UI.Xaml.Controls.Image()
                {
                    Stretch = GetStretch(Aspect.AspectFill)
                };
                control.ImageOpened += OnImageOpened;
                SetNativeControl(control);
            }

            if (e.OldElement != null)
            {
                e.OldElement.Cancelled -= Cancel;
            }
            if (e.NewElement != null)
            {
                e.NewElement.Cancelled += Cancel;
            }

            UpdateSource();
            UpdateAspect();
        }

        protected override void Dispose(bool disposing)
        {
            if (Control != null)
            {
                Control.ImageOpened -= OnImageOpened;
            }
            base.Dispose(disposing);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == CachedImage.SourceProperty.PropertyName)
            {
                UpdateSource();
            }
            else
            {
                if (!(e.PropertyName == CachedImage.AspectProperty.PropertyName))
                    return;

                UpdateAspect();
            }
        }

        private void OnImageOpened(object sender, RoutedEventArgs routedEventArgs)
        {
            if (measured)
            {
                ((IVisualElementController)Element).NativeSizeChanged();
            }
        }

        private async void UpdateSource()
        {
            ((IElementController)Element).SetValueFromRenderer(CachedImage.IsLoadingPropertyKey, true);

            Xamarin.Forms.ImageSource source = Element.Source;

            TaskParameter imageLoader = null;

            var ffSource = await ImageSourceBinding.GetImageSourceBinding(source);

            if (ffSource == null)
            {
                if (Control != null)
                    Control.Source = null;

                ImageLoadingFinished(Element);
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.Url)
            {
                imageLoader = ImageService.LoadUrl(ffSource.Path, Element.CacheDuration);
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.CompiledResource)
            {
                imageLoader = ImageService.LoadCompiledResource(ffSource.Path);
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.ApplicationBundle)
            {
                imageLoader = ImageService.LoadFileFromApplicationBundle(ffSource.Path);
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.Filepath)
            {
                imageLoader = ImageService.LoadFile(ffSource.Path);
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.Stream)
            {
                imageLoader = ImageService.LoadStream(ffSource.Stream);
            }

            if (imageLoader != null)
            {
                // LoadingPlaceholder
                if (Element.LoadingPlaceholder != null)
                {
                    var placeholderSource = await ImageSourceBinding.GetImageSourceBinding(Element.LoadingPlaceholder);
                    if (placeholderSource != null)
                        imageLoader.LoadingPlaceholder(placeholderSource.Path, placeholderSource.ImageSource);
                }

                // ErrorPlaceholder
                if (Element.ErrorPlaceholder != null)
                {
                    var placeholderSource = await ImageSourceBinding.GetImageSourceBinding(Element.ErrorPlaceholder);
                    if (placeholderSource != null)
                        imageLoader.ErrorPlaceholder(placeholderSource.Path, placeholderSource.ImageSource);
                }

                // Downsample
                if ((int)Element.DownsampleHeight != 0 || (int)Element.DownsampleWidth != 0)
                {
                    if (Element.DownsampleHeight > Element.DownsampleWidth)
                    {
                        imageLoader.DownSample(height: (int)Element.DownsampleWidth);
                    }
                    else
                    {
                        imageLoader.DownSample(width: (int)Element.DownsampleHeight);
                    }
                }

                // RetryCount
                if (Element.RetryCount > 0)
                {
                    imageLoader.Retry(Element.RetryCount, Element.RetryDelay);
                }

                // TransparencyChannel
                if (Element.TransparencyEnabled.HasValue)
                    imageLoader.TransparencyChannel(Element.TransparencyEnabled.Value);

                // FadeAnimation
                if (Element.FadeAnimationEnabled.HasValue)
                    imageLoader.FadeAnimation(Element.FadeAnimationEnabled.Value);

                // Transformations
                if (Element.Transformations != null)
                {
                    imageLoader.Transform(Element.Transformations);
                }

                imageLoader.Finish((work) => ImageLoadingFinished(Element));
                _currentTask = imageLoader.Into(Control);
            }
        }

        private void UpdateAspect()
        {
            Control.Stretch = GetStretch(Element.Aspect);
        }

        private static Stretch GetStretch(Aspect aspect)
        {
            switch (aspect)
            {
                case Aspect.AspectFill:
                    return Stretch.UniformToFill;
                case Aspect.Fill:
                    return Stretch.Fill;
                default:
                    return Stretch.Uniform;
            }
        }

        void ImageLoadingFinished(CachedImage element)
        {
            if (element != null)
            {
                ((IElementController)element).SetValueFromRenderer(CachedImage.IsLoadingPropertyKey, false);
                ((IVisualElementController)element).NativeSizeChanged();
				element.InvalidateViewMeasure();
            }
        }

        public void Cancel(object sender, EventArgs args)
        {
            if (_currentTask != null && !_currentTask.IsCancelled)
            {
                _currentTask.Cancel();
            }
        }
    }
}
