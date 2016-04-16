using FFImageLoading.Extensions;
using FFImageLoading.Work;
using System;
using System.Collections.Generic;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using System.Windows.Input;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

#if SILVERLIGHT
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
#endif

namespace FFImageLoading
{
	public sealed class FFImage : ContentControl, IDisposable
    {
        private Image internalImage;
        private IScheduledWork _currentTask;

        public FFImage()
		{
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            internalImage = new Image() {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Opacity = 1.0f,
            };
            Content = internalImage;

			Transformations = new List<ITransformation>();
            DownsampleMode = InterpolationMode.Default;
        }

        public Image Image
        {
            get
            {
                return internalImage;
            }
            set
            {
                internalImage = value;
                Content = internalImage;
            }
        }

        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(string), typeof(FFImage), new PropertyMetadata(null, SourcePropertyChanged));

        private static void SourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FFImage)d).SourcePropertyChanged((string)e.NewValue);
        }

        private void SourcePropertyChanged(string source)
        {
            if (IsInDesignMode)
                return;

            LoadImage();
        }

        private async void LoadImage()
        {
            if (_currentTask != null)
                _currentTask.Cancel();

            TaskParameter imageLoader = null;

            var ffSource = await FFImageSourceBinding.GetImageSourceBinding(Source);

            if (ffSource == null)
            {
                if (internalImage != null)
                {
                    await MainThreadDispatcher.Instance.PostAsync(() => {
                        internalImage.Source = null;
					});
                }
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.Url)
            {
                imageLoader = ImageService.Instance.LoadUrl(ffSource.Path, TimeSpan.FromDays(CacheDuration));
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.CompiledResource)
            {
                imageLoader = ImageService.Instance.LoadCompiledResource(ffSource.Path);
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.ApplicationBundle)
            {
                imageLoader = ImageService.Instance.LoadFileFromApplicationBundle(ffSource.Path);
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.Filepath)
            {
                imageLoader = ImageService.Instance.LoadFile(ffSource.Path);
            }

            if (imageLoader != null)
            {
				// CustomKeyFactory
				if (CacheKeyFactory != null)
				{
                    var dataContext = DataContext;
                    imageLoader.CacheKey(CacheKeyFactory.GetKey(Source, dataContext));
                }

                // LoadingPlaceholder
                if (LoadingPlaceholder != null)
                {
                    var placeholderSource = await FFImageSourceBinding.GetImageSourceBinding(LoadingPlaceholder);
                    if (placeholderSource != null)
                        imageLoader.LoadingPlaceholder(placeholderSource.Path, placeholderSource.ImageSource);
                }

                // ErrorPlaceholder
                if (ErrorPlaceholder != null)
                {
                    var placeholderSource = await FFImageSourceBinding.GetImageSourceBinding(ErrorPlaceholder);
                    if (placeholderSource != null)
                        imageLoader.ErrorPlaceholder(placeholderSource.Path, placeholderSource.ImageSource);
                }

                // Downsample
                if (DownsampleToViewSize && (Width > 0 || Height > 0))
                {
                    if (Height > Width)
                    {
                        imageLoader.DownSample(height: Height.PointsToPixels());
                    }
                    else
                    {
                        imageLoader.DownSample(width: Width.PointsToPixels());
                    }
                }
                else if (DownsampleToViewSize && (MinWidth > 0 || MinHeight > 0))
                {
                    if (MinHeight > MinWidth)
                    {
                        imageLoader.DownSample(height: MinHeight.PointsToPixels());
                    }
                    else
                    {
                        imageLoader.DownSample(width: MinWidth.PointsToPixels());
                    }
                }
                else if ((int)DownsampleHeight != 0 || (int)DownsampleWidth != 0)
                {
                    if (DownsampleHeight > DownsampleWidth)
                    {
                        imageLoader.DownSample(height: DownsampleUseDipUnits
                            ? DownsampleHeight.PointsToPixels() : (int)DownsampleHeight);
                    }
                    else
                    {
                        imageLoader.DownSample(width: DownsampleUseDipUnits
                            ? DownsampleWidth.PointsToPixels() : (int)DownsampleWidth);
                    }
                }

                // Downsample mode
                imageLoader.DownSampleMode(DownsampleMode);

                // RetryCount
                if (RetryCount > 0)
                {
                    imageLoader.Retry(RetryCount, RetryDelay);
                }

                // FadeAnimation
                imageLoader.FadeAnimation(FadeAnimationEnabled);

                // TransformPlaceholders
                imageLoader.TransformPlaceholders(TransformPlaceholders);

                // Transformations
                if (Transformations != null && Transformations.Count != 0)
                {
                    imageLoader.Transform(Transformations);
                }

                imageLoader.WithPriority(LoadingPriority);
                imageLoader.WithCache(CacheType);

                imageLoader.Finish((work) => 
                    OnFinish(new Args.FinishEventArgs(work)));

                imageLoader.Success((imageInformation, loadingResult) =>
                            OnSuccess(new Args.SuccessEventArgs(imageInformation, loadingResult)));

                imageLoader.Error((exception) =>
                    OnError(new Args.ErrorEventArgs(exception)));

                _currentTask = imageLoader.Into(internalImage);
            }
        }

        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(nameof(Stretch), typeof(Stretch), typeof(FFImage), new PropertyMetadata(default(Stretch), StretchPropertyChanged));

        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        private static void StretchPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FFImage)d).StretchPropertyChanged((Stretch)e.NewValue);
        }

        private void StretchPropertyChanged(Stretch stretch)
        {
            internalImage.Stretch = stretch;
        }

        /// <summary>
        /// The retry count property.
        /// </summary>
        public static readonly DependencyProperty RetryCountProperty = DependencyProperty.Register(nameof(RetryCount), typeof(int), typeof(FFImage), new PropertyMetadata(default(int)));

        /// <summary>
        /// If image loading fails automatically retry it a number of times, with a specific delay. Sets number of retries.
        /// </summary>
        public int RetryCount
        {
            get
            {
                return (int)GetValue(RetryCountProperty);
            }
            set
            {
                SetValue(RetryCountProperty, value);
            }
        }

        /// <summary>
        /// The retry delay property.
        /// </summary>
        public static readonly DependencyProperty RetryDelayProperty = DependencyProperty.Register(nameof(RetryDelay), typeof(int), typeof(FFImage), new PropertyMetadata(250));

        /// <summary>
        /// If image loading fails automatically retry it a number of times, with a specific delay. Sets delay in milliseconds between each trial
        /// </summary>
        public int RetryDelay
        {
            get
            {
                return (int)GetValue(RetryDelayProperty);
            }
            set
            {
                SetValue(RetryDelayProperty, value);
            }
        }

        /// <summary>
        /// The downsample width property.
        /// </summary>
        public static readonly DependencyProperty DownsampleWidthProperty = DependencyProperty.Register(nameof(DownsampleWidth), typeof(double), typeof(FFImage), new PropertyMetadata(default(double)));

        /// <summary>
        /// Reduce memory usage by downsampling the image. Aspect ratio will be kept even if width/height values are incorrect. 
        /// Optional DownsampleWidth parameter, if value is higher than zero it will try to downsample to this width while keeping aspect ratio.
        /// </summary>
        public double DownsampleWidth
        {
            get
            {
                return (double)GetValue(DownsampleWidthProperty);
            }
            set
            {
                SetValue(DownsampleWidthProperty, value);
            }
        }

        /// <summary>
        /// The downsample height property.
        /// </summary>
        public static readonly DependencyProperty DownsampleHeightProperty = DependencyProperty.Register(nameof(DownsampleHeight), typeof(double), typeof(FFImage), new PropertyMetadata(default(double)));

        /// <summary>
        /// Reduce memory usage by downsampling the image. Aspect ratio will be kept even if width/height values are incorrect. 
        /// Optional DownsampleHeight parameter, if value is higher than zero it will try to downsample to this height while keeping aspect ratio.
        /// </summary>
        public double DownsampleHeight
        {
            get
            {
                return (double)GetValue(DownsampleHeightProperty);
            }
            set
            {
                SetValue(DownsampleHeightProperty, value);
            }
        }

        public static readonly DependencyProperty DownsampleToViewSizeProperty = DependencyProperty.Register(nameof(DownsampleToViewSize), typeof(bool), typeof(FFImage), new PropertyMetadata(default(bool)));

        /// <summary>
        /// Reduce memory usage by downsampling the image. Aspect ratio will be kept even if width/height values are incorrect.
        /// DownsampleWidth and DownsampleHeight properties will be automatically set to view size
        /// If the view height or width will not return > 0 - it'll fall back 
        /// to using DownsampleWidth / DownsampleHeight properties values
        /// </summary>
        /// <value><c>true</c> if downsample to view size; otherwise, <c>false</c>.</value>
        public bool DownsampleToViewSize
        {
            get
            {
                return (bool)GetValue(DownsampleToViewSizeProperty);
            }
            set
            {
                SetValue(DownsampleToViewSizeProperty, value);
            }
        }

        //// <summary>
        /// The downsample interpolation mode property.
        /// </summary>
        public static readonly DependencyProperty DownsampleModeProperty = DependencyProperty.Register(nameof(DownsampleMode), typeof(InterpolationMode), typeof(FFImage), new PropertyMetadata(InterpolationMode.Default));

        /// <summary>
        /// Set interpolation (resizing) algorithm.
        /// </summary>
        /// <value>InterpolationMode enumeration. Bilinear by default.</value>
        public InterpolationMode DownsampleMode
        {
            get
            {
                return (InterpolationMode)GetValue(DownsampleModeProperty);
            }
            set
            {
                SetValue(DownsampleModeProperty, value);
            }
        }

        public static readonly DependencyProperty DownsampleUseDipUnitsProperty = DependencyProperty.Register(nameof(DownsampleUseDipUnits), typeof(bool), typeof(FFImage), new PropertyMetadata(default(bool)));

        /// <summary>
        /// If set to <c>true</c> DownsampleWidth and DownsampleHeight properties 
        /// will use density independent pixels for downsampling
        /// </summary>
        /// <value><c>true</c> if downsample use dip units; otherwise, <c>false</c>.</value>
        public bool DownsampleUseDipUnits
        {
            get
            {
                return (bool)GetValue(DownsampleUseDipUnitsProperty);
            }
            set
            {
                SetValue(DownsampleUseDipUnitsProperty, value);
            }
        }

        /// <summary>
        /// The cache duration property.
        /// </summary>
        public static readonly DependencyProperty CacheDurationProperty = DependencyProperty.Register(nameof(CacheDuration), typeof(int), typeof(FFImage), new PropertyMetadata(30));

        /// <summary>
        /// How long the file will be cached on disk.
        /// </summary>
        public int CacheDuration
        {
            get
            {
                return (int)GetValue(CacheDurationProperty);
            }
            set
            {
                SetValue(CacheDurationProperty, value);
            }
        }

        /// <summary>
        /// The Loading Priority property.
        /// </summary>
        public static readonly DependencyProperty LoadingPriorityProperty = DependencyProperty.Register(nameof(LoadingPriority), typeof(LoadingPriority), typeof(FFImage), new PropertyMetadata(LoadingPriority.Normal));

        /// <summary>
        /// Defines the loading priority, the default is LoadingPriority.Normal
        /// </summary>
        public LoadingPriority LoadingPriority
        {
            get
            {
                return (LoadingPriority)GetValue(LoadingPriorityProperty); 
            }
            set
            {
                SetValue(LoadingPriorityProperty, value); 
            }
        }

        /// <summary>
        /// The cache type property.
        /// </summary>
        public static readonly DependencyProperty CacheTypeProperty = DependencyProperty.Register(nameof(CacheType), typeof(CacheType), typeof(FFImage), new PropertyMetadata(CacheType.All));

        /// <summary>
        /// Set the cache storage type, (Memory, Disk, All). by default cache is set to All.
        /// </summary>
        public CacheType CacheType
        {
            get
            {
                return (CacheType)GetValue(CacheTypeProperty);
            }
            set
            {
                SetValue(CacheTypeProperty, value);
            }
        }
        

        /// <summary>
        /// The fade animation enabled property.
        /// </summary>
        public static readonly DependencyProperty FadeAnimationEnabledProperty = DependencyProperty.Register(nameof(FadeAnimationEnabled), typeof(bool), typeof(FFImage), new PropertyMetadata(true));

        /// <summary>
        /// Indicates if the fade animation effect should be enabled. By default this value comes from ImageService.Instance.Config.FadeAnimationEnabled.
        /// </summary>
        public bool FadeAnimationEnabled
        {
            get
            {
                return (bool)GetValue(FadeAnimationEnabledProperty);
            }
            set
            {
                SetValue(FadeAnimationEnabledProperty, value);
            }
        }

        /// <summary>
        /// The transform placeholders property.
        /// </summary>
        /// 
        public static readonly DependencyProperty TransformPlaceholdersProperty = DependencyProperty.Register(nameof(TransformPlaceholders), typeof(bool), typeof(FFImage), new PropertyMetadata(true));

        /// <summary>
        /// Indicates if transforms should be applied to placeholders.  By default this value comes from ImageService.Instance.Config.TransformPlaceholders.
        /// </summary>
        public bool TransformPlaceholders
        {
            get
            {
                return (bool)GetValue(TransformPlaceholdersProperty);
            }
            set
            {
                SetValue(TransformPlaceholdersProperty, value);
            }
        }

        /// <summary>
        /// The loading placeholder property.
        /// </summary>
        public static readonly DependencyProperty LoadingPlaceholderProperty = DependencyProperty.Register(nameof(LoadingPlaceholder), typeof(string), typeof(FFImage), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the loading placeholder image.
        /// </summary>
        public string LoadingPlaceholder
        {
            get
            {
                return (string)GetValue(LoadingPlaceholderProperty);
            }
            set
            {
                SetValue(LoadingPlaceholderProperty, value);
            }
        }

        /// <summary>
        /// The error placeholder property.
        /// </summary>
        public static readonly DependencyProperty ErrorPlaceholderProperty = DependencyProperty.Register(nameof(ErrorPlaceholder), typeof(string), typeof(FFImage), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the error placeholder image.
        /// </summary>
        public string ErrorPlaceholder
        {
            get
            {
                return (string)GetValue(ErrorPlaceholderProperty);
            }
            set
            {
                SetValue(ErrorPlaceholderProperty, value);
            }
        }

        /// <summary>
        /// The transformations property.
        /// </summary>
        public static readonly DependencyProperty TransformationsProperty = DependencyProperty.Register(nameof(Transformations), typeof(List<ITransformation>), typeof(FFImage), new PropertyMetadata(new List<ITransformation>(), TransformationsPropertyChanged));

        private static void TransformationsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FFImage)d).TransformationsPropertyChanged((List<ITransformation>)e.NewValue);
        }

        private void TransformationsPropertyChanged(List<ITransformation> transformations)
        {
            if (IsInDesignMode)
                return;

            LoadImage();
        }

        /// <summary>
        /// Gets or sets the transformations.
        /// </summary>
        /// <value>The transformations.</value>
        public List<ITransformation> Transformations
        {
            get
            {
                return (List<ITransformation>)GetValue(TransformationsProperty);
            }
            set
            {
                SetValue(TransformationsProperty, value);
            }
        }

        /// <summary>
        /// Cancels image loading tasks
        /// </summary>
        public void Cancel()
        {
            if (_currentTask != null && !_currentTask.IsCancelled)
            {
                _currentTask.Cancel();
            }
        }

        /// <summary>
        /// Gets the image as JPG.
        /// </summary>
        /// <returns>The image as JPG.</returns>
        public Task<byte[]> GetImageAsJpgAsync(int quality = 90, int desiredWidth = 0, int desiredHeight = 0)
        {
            return GetImageAsByteAsync(BitmapEncoder.JpegEncoderId, quality, desiredWidth, desiredHeight);
        }

        /// <summary>
        /// Gets the image as PNG
        /// </summary>
        /// <returns>The image as PNG.</returns>
        public Task<byte[]> GetImageAsPngAsync(int desiredWidth = 0, int desiredHeight = 0)
        {
            return GetImageAsByteAsync(BitmapEncoder.PngEncoderId, 90, desiredWidth, desiredHeight);
        }

        private async Task<byte[]> GetImageAsByteAsync(Guid format, int quality, int desiredWidth, int desiredHeight)
        {
            if (internalImage == null || internalImage.Source == null)
                return null;

            var bitmap = internalImage.Source as WriteableBitmap;

            if (bitmap == null)
                return null;

            byte[] pixels = null;
            uint pixelsWidth = (uint)bitmap.PixelWidth;
            uint pixelsHeight = (uint)bitmap.PixelHeight;

            if (desiredWidth != 0 || desiredHeight != 0)
            {
                double widthRatio = (double)desiredWidth / (double)bitmap.PixelWidth;
                double heightRatio = (double)desiredHeight / (double)bitmap.PixelHeight;

                double scaleRatio = Math.Min(widthRatio, heightRatio);

                if (desiredWidth == 0)
                    scaleRatio = heightRatio;

                if (desiredHeight == 0)
                    scaleRatio = widthRatio;

                uint aspectWidth = (uint)((double)bitmap.PixelWidth * scaleRatio);
                uint aspectHeight = (uint)((double)bitmap.PixelHeight * scaleRatio);

                using (var tempStream = new InMemoryRandomAccessStream())
                {
                    byte[] tempPixels = await GetBytesFromBitmapAsync(bitmap);

                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, tempStream);
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
                        pixelsWidth, pixelsHeight, 96, 96, tempPixels);
                    await encoder.FlushAsync();
                    tempStream.Seek(0);

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(tempStream);
                    BitmapTransform transform = new BitmapTransform()
                    {
                        ScaledWidth = aspectWidth,
                        ScaledHeight = aspectHeight,
                        InterpolationMode = BitmapInterpolationMode.Linear
                    };
                    PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied,
                        transform,
                        ExifOrientationMode.RespectExifOrientation,
                        ColorManagementMode.DoNotColorManage);

                    pixels = pixelData.DetachPixelData();
                    pixelsWidth = aspectWidth;
                    pixelsHeight = aspectHeight;
                }
            }
            else
            {
                pixels = await GetBytesFromBitmapAsync(bitmap);
            }

            using (var stream = new InMemoryRandomAccessStream())
            {
                var encoder = await BitmapEncoder.CreateAsync(format, stream);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
                    pixelsWidth, pixelsHeight, 96, 96, pixels);
                await encoder.FlushAsync();
                stream.Seek(0);

                var bytes = new byte[stream.Size];
                await stream.ReadAsync(bytes.AsBuffer(), (uint)stream.Size, InputStreamOptions.None);

                return bytes;
            }
        }

        private async Task<byte[]> GetBytesFromBitmapAsync(WriteableBitmap bitmap)
        {
#if SILVERLIGHT
            return await Task.FromResult(bitmap.ToByteArray());
#else
            byte[] tempPixels;
            using (var sourceStream = bitmap.PixelBuffer.AsStream())
            {
                tempPixels = new byte[sourceStream.Length];
                await sourceStream.ReadAsync(tempPixels, 0, tempPixels.Length).ConfigureAwait(false);
            }

            return tempPixels;
#endif
        }

        /// <summary>
        /// Gets or sets the cache custom key factory.
        /// </summary>
        /// <value>The cache key factory.</value>
        public ICacheKeyFactory CacheKeyFactory { get; set; }

        /// <summary>
        /// Pauses image loading (enable or disable).
        /// </summary>
        /// <param name="pauseWork">If set to <c>true</c> pauses image loading.</param>
        public static void SetPauseWork(bool pauseWork)
        {
            ImageService.Instance.SetPauseWork(pauseWork);
        }

        /// <summary>
        /// Clears image cache
        /// </summary>
        /// <param name="cacheType">Cache type to invalidate</param>
        public static async Task ClearCacheAsync(CacheType cacheType)
        {
            switch (cacheType)
            {
                case CacheType.Memory:
                    ImageService.Instance.InvalidateMemoryCache();
                    break;
                case CacheType.Disk:
                    await ImageService.Instance.InvalidateDiskCacheAsync().ConfigureAwait(false);
                    break;
                case CacheType.All:
                    ImageService.Instance.InvalidateMemoryCache();
                    await ImageService.Instance.InvalidateDiskCacheAsync().ConfigureAwait(false);
                    break;
            }
        }

        /// <summary>
        /// Invalidates cache for a specified key
        /// </summary>
        /// <param name="key">Key to invalidate</param>
        /// <param name="cacheType">Cache type to invalidate</param>
        public static async Task InvalidateCacheEntryAsync(string key, CacheType cacheType)
        {
            await ImageService.Instance.InvalidateCacheEntryAsync(key, cacheType);
        }

        /// <summary>
        /// Occurs after image loading success.
        /// </summary>
        public event EventHandler<Args.SuccessEventArgs> Success;

        /// <summary>
        /// The SuccessCommandProperty.
        /// </summary>
        public static readonly DependencyProperty SuccessCommandProperty = DependencyProperty.Register("SuccessCommand",
            typeof(ICommand), typeof(FFImage), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the SuccessCommand.
        /// Occurs after image loading success.
        /// Command parameter: CachedImageEvents.SuccessEventArgs
        /// </summary>
        /// <value>The success command.</value>
        public ICommand SuccessCommand
        {
            get
            {
                return (ICommand)GetValue(SuccessCommandProperty);
            }
            set
            {
                SetValue(SuccessCommandProperty, value);
            }
        }

        internal void OnSuccess(Args.SuccessEventArgs e)
        {
            var handler = Success;
            if (handler != null) handler(this, e);

            var successCommand = SuccessCommand;
            if (successCommand != null && successCommand.CanExecute(e))
                successCommand.Execute(e);
        }

        /// <summary>
        /// Occurs after image loading error.
        /// </summary>
        public event EventHandler<Args.ErrorEventArgs> Error;

        /// <summary>
        /// The ErrorCommandProperty.
        /// </summary>
        public static readonly DependencyProperty ErrorCommandProperty = DependencyProperty.Register("ErrorCommand",
            typeof(ICommand), typeof(FFImage), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the ErrorCommand.
        /// Occurs after image loading error.
        /// Command parameter: CachedImageEvents.ErrorEventArgs
        /// </summary>
        /// <value>The error command.</value>
        public ICommand ErrorCommand
        {
            get
            {
                return (ICommand)GetValue(ErrorCommandProperty);
            }
            set
            {
                SetValue(ErrorCommandProperty, value);
            }
        }

        internal void OnError(Args.ErrorEventArgs e)
        {
            var handler = Error;
            if (handler != null) handler(this, e);

            var errorCommand = ErrorCommand;
            if (errorCommand != null && errorCommand.CanExecute(e))
                errorCommand.Execute(e);
        }

        /// <summary>
        /// Occurs after every image loading.
        /// </summary>
        public event EventHandler<Args.FinishEventArgs> Finish;

        /// <summary>
        /// The FinishCommandProperty.
        /// </summary>
        public static readonly DependencyProperty FinishCommandProperty = DependencyProperty.Register("FinishCommand",
            typeof(ICommand), typeof(FFImage), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the FinishCommand.
        /// Occurs after every image loading.
        /// Command parameter: CachedImageEvents.FinishEventArgs
        /// </summary>
        /// <value>The finish command.</value>
        public ICommand FinishCommand
        {
            get
            {
                return (ICommand)GetValue(FinishCommandProperty);
            }
            set
            {
                SetValue(FinishCommandProperty, value);
            }
        }

        internal void OnFinish(Args.FinishEventArgs e)
        {
            var handler = Finish;
            if (handler != null) handler(this, e);

            var finishCommand = FinishCommand;
            if (finishCommand != null && finishCommand.CanExecute(e))
                finishCommand.Execute(e);
        }

        public void Dispose()
        {
            internalImage.Source = null;
        }

        private bool IsInDesignMode
        {
            get
            {
#if SILVERLIGHT
                return Application.Current.RootVisual != null && DesignerProperties.GetIsInDesignMode(Application.Current.RootVisual);
#else
                return Windows.ApplicationModel.DesignMode.DesignModeEnabled;
#endif
            }
        }
    }
}
