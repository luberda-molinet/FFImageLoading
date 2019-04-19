using FFImageLoading.Extensions;
using FFImageLoading.Forms.Args;
using FFImageLoading.Helpers;
using FFImageLoading.Work;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;
using Image = System.Windows.Controls.Image;
using Size = Xamarin.Forms.Size;

namespace FFImageLoading.Forms.Platform
{
	/// <summary>
	/// CachedImage Implementation
	/// </summary>
	[Preserve(AllMembers = true)]
	public class CachedImageRenderer : ViewRenderer<CachedImage, Image>
    {
        [RenderWith(typeof(CachedImageRenderer))]
        internal class _CachedImageRenderer
        {
        }

        bool _isSizeSet;
        private bool _measured;
        private IScheduledWork _currentTask;
        private ImageSourceBinding _lastImageSource;
        private bool _isDisposed = false;

        /// <summary>
        ///   Used for registration with dependency service
        /// </summary>
        public static void Init()
        {
            CachedImage.IsRendererInitialized = true;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            ScaleHelper.InitAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        public override SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            var bitmapSource = Control.Source as BitmapSource;

            if (bitmapSource == null)
                return new SizeRequest();

            _measured = true;

            return new SizeRequest(new Size()
            {
                Width = bitmapSource.PixelWidth,
                Height = bitmapSource.PixelHeight
            });
        }

        protected override void OnElementChanged(ElementChangedEventArgs<CachedImage> e)
        {
            base.OnElementChanged(e);

            if (Control == null && Element != null && !_isDisposed)
            {
                var control = new Image()
                {
                    Stretch = GetStretch(Aspect.AspectFill),
                };
                control.Loaded += OnImageOpened;
                SetNativeControl(control);

                Control.HorizontalAlignment = HorizontalAlignment.Center;
                Control.VerticalAlignment = VerticalAlignment.Center;
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
                _isSizeSet = false;
                e.NewElement.InternalReloadImage = new Action(ReloadImage);
                e.NewElement.InternalCancel = new Action(CancelIfNeeded);
                e.NewElement.InternalGetImageAsJPG = new Func<GetImageAsJpgArgs, Task<byte[]>>(GetImageAsJpgAsync);
                e.NewElement.InternalGetImageAsPNG = new Func<GetImageAsPngArgs, Task<byte[]>>(GetImageAsPngAsync);

                UpdateAspect();
                UpdateImage(Control, Element, e.OldElement);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                if (Control != null)
                {
                    Control.Loaded -= OnImageOpened;
                }
                CancelIfNeeded();
            }

            base.Dispose(disposing);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == CachedImage.SourceProperty.PropertyName)
            {
                UpdateImage(Control, Element, null);
            }
            else if (e.PropertyName == CachedImage.AspectProperty.PropertyName)
            {
                UpdateAspect();
            }
        }

        void OnImageOpened(object sender, RoutedEventArgs routedEventArgs)
        {
            if (_measured)
            {
                ((IVisualElementController)Element)?.InvalidateMeasure(Xamarin.Forms.Internals.InvalidationTrigger.RendererReady);
            }
        }

        async void UpdateImage(Image imageView, CachedImage image, CachedImage previousImage)
        {
            CancelIfNeeded();

            if (image == null || imageView == null || _isDisposed)
                return;

            var ffSource = await ImageSourceBinding.GetImageSourceBinding(image.Source, image).ConfigureAwait(false);
            if (ffSource == null)
            {
                if (_lastImageSource == null)
                    return;

                _lastImageSource = null;
                imageView.Source = null;
                return;
            }

            if (previousImage != null && !ffSource.Equals(_lastImageSource))
            {
                _lastImageSource = null;
                imageView.Source = null;
            }

            image.SetIsLoading(true);

            var placeholderSource = await ImageSourceBinding.GetImageSourceBinding(image.LoadingPlaceholder, image).ConfigureAwait(false);
            var errorPlaceholderSource = await ImageSourceBinding.GetImageSourceBinding(image.ErrorPlaceholder, image).ConfigureAwait(false);
            TaskParameter imageLoader;
            image.SetupOnBeforeImageLoading(out imageLoader, ffSource, placeholderSource, errorPlaceholderSource);

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

        void UpdateAspect()
        {
            if (Control == null || Element == null || _isDisposed)
                return;
            Control.Stretch = GetStretch(Element.Aspect);
        }

        static Stretch GetStretch(Aspect aspect)
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

        async void ImageLoadingSizeChanged(CachedImage element, bool isLoading)
        {
            if (element != null && !_isDisposed)
			{
				await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() =>
				{
					if (!isLoading || !_isSizeSet)
                    {
                        ((IVisualElementController)element)?.InvalidateMeasure(Xamarin.Forms.Internals.InvalidationTrigger.RendererReady);
                        _isSizeSet = true;
                    }

                    if (!isLoading)
                        element.SetIsLoading(isLoading);
				});
			}
        }

        void ReloadImage()
        {
            UpdateImage(Control, Element, null);
        }

        void CancelIfNeeded()
        {
            try
            {
                _currentTask?.Cancel();
            }
            catch (Exception) { }
        }

        Task<byte[]> GetImageAsJpgAsync(GetImageAsJpgArgs args)
        {
            return GetImageAsByteAsync(new JpegBitmapEncoder(), args.Quality, args.DesiredWidth, args.DesiredHeight);
        }

        Task<byte[]> GetImageAsPngAsync(GetImageAsPngArgs args)
        {
            return GetImageAsByteAsync(new PngBitmapEncoder(), 90, args.DesiredWidth, args.DesiredHeight);
        }

        async Task<byte[]> GetImageAsByteAsync(BitmapEncoder encoder, int quality, int desiredWidth, int desiredHeight)
        {
            if (Control?.Source == null)
                return null;

            var bitmap = Control.Source as WriteableBitmap;

            if (bitmap == null)
                return null;

            //byte[] pixels = null;
            //uint pixelsWidth = (uint)bitmap.PixelWidth;
            //uint pixelsHeight = (uint)bitmap.PixelHeight;

            WriteableBitmap source;
            if (desiredWidth != 0 || desiredHeight != 0)
            {
                double widthRatio = (double)desiredWidth / (double)bitmap.PixelWidth;
                double heightRatio = (double)desiredHeight / (double)bitmap.PixelHeight;

                double scaleRatio = Math.Min(widthRatio, heightRatio);

                if (desiredWidth == 0)
                    scaleRatio = heightRatio;

                if (desiredHeight == 0)
                    scaleRatio = widthRatio;

                var tr = new TransformedBitmap(bitmap, new ScaleTransform(scaleRatio, scaleRatio));

                var bmp = new MemoryStream();
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(tr));
                enc.Save(bmp);
                bmp.Seek(0, SeekOrigin.Begin);
                //pixels = new byte[bmp.Length];
                source = await bmp.ToWriteableBitmap();
                //await bmp.ReadAsync(pixels, 0, (int)bmp.Length);
            }
            else
            {
                //pixels = await GetBytesFromBitmapAsync(bitmap);
                source = bitmap;
            }

            using (var stream = new MemoryStream())
            {
                if (encoder is JpegBitmapEncoder enc)
                {
                    //var propertySet = new BitmapPropertySet();
                    //var qualityValue = new BitmapTypedValue((double)quality / 100d, Windows.Foundation.PropertyType.Single);
                    //propertySet.Add("ImageQuality", qualityValue);

                    //encoder = await BitmapEncoder.CreateAsync(format, stream, propertySet);
                    enc.QualityLevel = quality;
                }
                //else
                //{
                //    encoder = await BitmapEncoder.CreateAsync(format, stream);
                //}

                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(stream);
                //encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
                //    pixelsWidth, pixelsHeight, 96, 96, pixels);
                //await encoder.FlushAsync();
                stream.Seek(0,SeekOrigin.Begin);

                var bytes = new byte[stream.Length];
                await stream.ReadAsync(bytes, 0,bytes.Length);

                return bytes;
            }
        }

        async Task<byte[]> GetBytesFromBitmapAsync(WriteableBitmap bitmap)
        {
            var width = bitmap.PixelWidth;
            var height = bitmap.PixelHeight;
            var stride = width * ((bitmap.Format.BitsPerPixel + 7) / 8);

            var bitmapData = new byte[height * stride];

            bitmap.CopyPixels(bitmapData, stride, 0);
            return bitmapData;
            //byte[] tempPixels;
            //using (var sourceStream = bitmap.PixelBuffer.AsStream())
            //{
            //    tempPixels = new byte[sourceStream.Length];
            //    await sourceStream.ReadAsync(tempPixels, 0, tempPixels.Length).ConfigureAwait(false);
            //}

            //return tempPixels;
        }
    }
}
