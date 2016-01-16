using FFImageLoading.Extensions;
using FFImageLoading.Work;
using System;
using System.Collections.Generic;
using Windows.UI.Core;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;

#if SILVERLIGHT
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
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

        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register("Source", 
            typeof(string), typeof(FFImage), new PropertyMetadata(null, SourcePropertyChanged));

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
                imageLoader = ImageService.LoadUrl(ffSource.Path, TimeSpan.FromDays(CacheDuration));
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

                _currentTask = imageLoader.Into(internalImage);
            }
        }

        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register("Stretch",
            typeof(Stretch), typeof(FFImage), new PropertyMetadata(default(Stretch), StretchPropertyChanged));

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
        public static readonly DependencyProperty RetryCountProperty = DependencyProperty.Register("RetryCount",
            typeof(int), typeof(FFImage), new PropertyMetadata(default(int)));

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
        public static readonly DependencyProperty RetryDelayProperty = DependencyProperty.Register("RetryDelay",
            typeof(int), typeof(FFImage), new PropertyMetadata(250));

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
        public static readonly DependencyProperty DownsampleWidthProperty = DependencyProperty.Register("DownsampleWidth",
            typeof(double), typeof(FFImage), new PropertyMetadata(default(double)));

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
        public static readonly DependencyProperty DownsampleHeightProperty = DependencyProperty.Register("DownsampleHeight",
            typeof(double), typeof(FFImage), new PropertyMetadata(default(double)));

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

        public static readonly DependencyProperty DownsampleToViewSizeProperty = DependencyProperty.Register("DownsampleToViewSize",
            typeof(bool), typeof(FFImage), new PropertyMetadata(default(bool)));

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
        public static readonly DependencyProperty DownsampleModeProperty =
            DependencyProperty.Register("DownsampleMode", typeof(InterpolationMode), typeof(FFImage), new PropertyMetadata(InterpolationMode.Default));

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

        public static readonly DependencyProperty DownsampleUseDipUnitsProperty = DependencyProperty.Register("DownsampleUseDipUnits",
            typeof(bool), typeof(FFImage), new PropertyMetadata(default(bool)));

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
        public static readonly DependencyProperty CacheDurationProperty = DependencyProperty.Register("CacheDuration",
            typeof(int), typeof(FFImage), new PropertyMetadata(30));

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
        /// The fade animation enabled property.
        /// </summary>
        public static readonly DependencyProperty FadeAnimationEnabledProperty = DependencyProperty.Register("FadeAnimationEnabled",
            typeof(bool), typeof(FFImage), new PropertyMetadata(true));

        /// <summary>
        /// Indicates if the fade animation effect should be enabled. By default this value comes from ImageService.Config.FadeAnimationEnabled.
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
        public static readonly DependencyProperty TransformPlaceholdersProperty =
            DependencyProperty.Register("TransformPlaceholders", typeof(bool), typeof(FFImage), new PropertyMetadata(true));

        /// <summary>
        /// Indicates if transforms should be applied to placeholders.  By default this value comes from ImageService.Config.TransformPlaceholders.
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
        public static readonly DependencyProperty LoadingPlaceholderProperty = DependencyProperty.Register("LoadingPlaceholder",
            typeof(string), typeof(FFImage), new PropertyMetadata(null));

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
        public static readonly DependencyProperty ErrorPlaceholderProperty = DependencyProperty.Register("ErrorPlaceholder",
            typeof(string), typeof(FFImage), new PropertyMetadata(null));

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
        public static readonly DependencyProperty TransformationsProperty = DependencyProperty.Register("Transformations",
            typeof(List<FFImageLoading.Work.ITransformation>), typeof(FFImage), new PropertyMetadata(new List<FFImageLoading.Work.ITransformation>(), TransformationsPropertyChanged));

        private static void TransformationsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FFImage)d).TransformationsPropertyChanged((List<FFImageLoading.Work.ITransformation>)e.NewValue);
        }

        private void TransformationsPropertyChanged(List<FFImageLoading.Work.ITransformation> transformations)
        {
            if (IsInDesignMode)
                return;

            LoadImage();
        }

        /// <summary>
        /// Gets or sets the transformations.
        /// </summary>
        /// <value>The transformations.</value>
        public List<FFImageLoading.Work.ITransformation> Transformations
        {
            get
            {
                return (List<FFImageLoading.Work.ITransformation>)GetValue(TransformationsProperty);
            }
            set
            {
                SetValue(TransformationsProperty, value);
            }
        }

		/// <summary>
		/// Gets or sets the cache custom key factory.
		/// </summary>
		/// <value>The cache key factory.</value>
		public ICacheKeyFactory CacheKeyFactory { get; set; }

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

