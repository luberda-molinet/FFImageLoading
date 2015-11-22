using FFImageLoading.Work;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace FFImageLoading
{
	public sealed class FFImage : ContentControl, IDisposable
    {
        private Image internalImage;
        private IScheduledWork _currentTask;

        public FFImage()
		{
            internalImage = new Image();
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Content = internalImage;
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
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
                return;

            ((FFImage)d).SourcePropertyChanged((string)e.NewValue);
        }

        private async void SourcePropertyChanged(string source)
        {
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
                return;

            TaskParameter imageLoader = null;

            var ffSource = await FFImageSourceBinding.GetImageSourceBinding(source);

            if (ffSource == null)
            {
                if (internalImage != null)
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => {
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
                if ((int)DownsampleHeight != 0 || (int)DownsampleWidth != 0)
                {
                    if (DownsampleHeight > DownsampleWidth)
                    {
                        imageLoader.DownSample(height: (int)DownsampleWidth);
                    }
                    else
                    {
                        imageLoader.DownSample(width: (int)DownsampleHeight);
                    }
                }

                // RetryCount
                if (RetryCount > 0)
                {
                    imageLoader.Retry(RetryCount, RetryDelay);
                }

                // TransparencyChannel
                if (TransparencyEnabled.HasValue)
                    imageLoader.TransparencyChannel(TransparencyEnabled.Value);

                // FadeAnimation
                if (FadeAnimationEnabled.HasValue)
                    imageLoader.FadeAnimation(FadeAnimationEnabled.Value);

                // Transformations
                if (Transformations != null)
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
        /// The transparency enabled property.
        /// </summary>
        public static readonly DependencyProperty TransparencyEnabledProperty = DependencyProperty.Register("TransparencyEnabled",
            typeof(bool?), typeof(FFImage), new PropertyMetadata(null));

        /// <summary>
        /// Indicates if the transparency channel should be loaded. By default this value comes from ImageService.Config.LoadWithTransparencyChannel.
        /// </summary>
        public bool? TransparencyEnabled
        {
            get
            {
                return (bool?)GetValue(TransparencyEnabledProperty);
            }
            set
            {
                SetValue(TransparencyEnabledProperty, value);
            }
        }

        /// <summary>
        /// The fade animation enabled property.
        /// </summary>
        public static readonly DependencyProperty FadeAnimationEnabledProperty = DependencyProperty.Register("FadeAnimationEnabled",
            typeof(bool?), typeof(FFImage), new PropertyMetadata(null));

        /// <summary>
        /// Indicates if the fade animation effect should be enabled. By default this value comes from ImageService.Config.FadeAnimationEnabled.
        /// </summary>
        public bool? FadeAnimationEnabled
        {
            get
            {
                return (bool?)GetValue(FadeAnimationEnabledProperty);
            }
            set
            {
                SetValue(FadeAnimationEnabledProperty, value);
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
            typeof(List<FFImageLoading.Work.ITransformation>), typeof(FFImage), new PropertyMetadata(null));

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

        public void Dispose()
        {
            internalImage.Source = null;
        }
    }
}

