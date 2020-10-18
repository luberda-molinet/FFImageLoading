using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFImageLoading.Args;
using FFImageLoading.Cache;
using FFImageLoading.Work;
using Windows.UI.Xaml.Controls;
using System.ComponentModel;
using Windows.UI.Xaml;
using System.IO;
using System.Threading;

namespace FFImageLoading.Views
{
    public class MvxCachedImageView : ContentControl, ICachedImageView, IDisposable
    {
        private Image _internalImage;
        protected IScheduledWork _scheduledWork;
        protected ImageSourceBinding _lastImageSource;
        protected bool _isDisposed;

        /// <summary>
        /// MvxCachedImageView by Daniel Luberda
        /// </summary>
        public MvxCachedImageView()
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            _internalImage = new Image()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Opacity = 1.0f,
            };
            Content = _internalImage;

            Transformations = new List<ITransformation>();
        }

        public Image Image
        {
            get
            {
                return _internalImage;
            }
            set
            {
                _internalImage = value;
                Content = _internalImage;
            }
        }

        protected static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (MvxCachedImageView)d;
            if (view.IsInDesignMode)
                return;

            view.UpdateImageLoadingTask();
        }

        protected static void OnTransformationsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (MvxCachedImageView)d;
            if (view.IsInDesignMode)
                return;

            if (view._lastImageSource != null)
            {
                view.UpdateImageLoadingTask();
            }
        }

        bool IsInDesignMode => Windows.ApplicationModel.DesignMode.DesignModeEnabled;

        public bool IsLoading { get { return (bool)GetValue(IsLoadingProperty); } set { SetValue(IsLoadingProperty, value); } }
        public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(MvxCachedImageView), new PropertyMetadata(default(bool)));

        public int RetryCount { get { return (int)GetValue(RetryCountProperty); } set { SetValue(RetryCountProperty, value); } }
        public static readonly DependencyProperty RetryCountProperty = DependencyProperty.Register(nameof(RetryCount), typeof(int), typeof(MvxCachedImageView), new PropertyMetadata(3));

        public int RetryDelay { get { return (int)GetValue(RetryDelayProperty); } set { SetValue(RetryDelayProperty, value); } }
        public static readonly DependencyProperty RetryDelayProperty = DependencyProperty.Register(nameof(RetryDelay), typeof(int), typeof(MvxCachedImageView), new PropertyMetadata(500));

        public int LoadingDelay { get { return (int)GetValue(LoadingDelayProperty); } set { SetValue(LoadingDelayProperty, value); } }
        public static readonly DependencyProperty LoadingDelayProperty = DependencyProperty.Register(nameof(LoadingDelay), typeof(int), typeof(MvxCachedImageView), new PropertyMetadata(default(int)));

        public double DownsampleWidth { get { return (double)GetValue(DownsampleWidthProperty); } set { SetValue(DownsampleWidthProperty, value); } }
        public static readonly DependencyProperty DownsampleWidthProperty = DependencyProperty.Register(nameof(DownsampleWidth), typeof(double), typeof(MvxCachedImageView), new PropertyMetadata(default(double)));

        public double DownsampleHeight { get { return (double)GetValue(DownsampleHeightProperty); } set { SetValue(DownsampleHeightProperty, value); } }
        public static readonly DependencyProperty DownsampleHeightProperty = DependencyProperty.Register(nameof(DownsampleHeight), typeof(double), typeof(MvxCachedImageView), new PropertyMetadata(default(double)));

        public bool DownsampleUseDipUnits { get { return (bool)GetValue(DownsampleUseDipUnitsProperty); } set { SetValue(DownsampleUseDipUnitsProperty, value); } }
        public static readonly DependencyProperty DownsampleUseDipUnitsProperty = DependencyProperty.Register(nameof(DownsampleUseDipUnits), typeof(bool), typeof(MvxCachedImageView), new PropertyMetadata(default(bool)));

        public TimeSpan? CacheDuration { get { return (TimeSpan?)GetValue(CacheDurationProperty); } set { SetValue(CacheDurationProperty, value); } }
        public static readonly DependencyProperty CacheDurationProperty = DependencyProperty.Register(nameof(CacheDuration), typeof(TimeSpan?), typeof(MvxCachedImageView), new PropertyMetadata(default(TimeSpan?)));

        public LoadingPriority LoadingPriority { get { return (LoadingPriority)GetValue(LoadingPriorityProperty); } set { SetValue(LoadingPriorityProperty, value); } }
        public static readonly DependencyProperty LoadingPriorityProperty = DependencyProperty.Register(nameof(LoadingPriority), typeof(LoadingPriority), typeof(MvxCachedImageView), new PropertyMetadata(default(LoadingPriority)));

        public bool? BitmapOptimizations { get { return (bool?)GetValue(BitmapOptimizationsProperty); } set { SetValue(BitmapOptimizationsProperty, value); } }
        public static readonly DependencyProperty BitmapOptimizationsProperty = DependencyProperty.Register(nameof(BitmapOptimizations), typeof(bool?), typeof(MvxCachedImageView), new PropertyMetadata(default(bool?)));

        public bool? FadeAnimationEnabled { get { return (bool?)GetValue(FadeAnimationEnabledProperty); } set { SetValue(FadeAnimationEnabledProperty, value); } }
        public static readonly DependencyProperty FadeAnimationEnabledProperty = DependencyProperty.Register(nameof(FadeAnimationEnabled), typeof(bool?), typeof(MvxCachedImageView), new PropertyMetadata(default(bool?)));

        public bool? FadeAnimationForCachedImages { get { return (bool?)GetValue(FadeAnimationForCachedImagesProperty); } set { SetValue(FadeAnimationForCachedImagesProperty, value); } }
        public static readonly DependencyProperty FadeAnimationForCachedImagesProperty = DependencyProperty.Register(nameof(FadeAnimationForCachedImages), typeof(bool?), typeof(MvxCachedImageView), new PropertyMetadata(default(bool?)));

        public bool? InvalidateLayoutAfterLoaded { get { return (bool?)GetValue(InvalidateLayoutAfterLoadedProperty); } set { SetValue(InvalidateLayoutAfterLoadedProperty, value); } }
        public static readonly DependencyProperty InvalidateLayoutAfterLoadedProperty = DependencyProperty.Register(nameof(InvalidateLayoutAfterLoaded), typeof(bool?), typeof(MvxCachedImageView), new PropertyMetadata(default(bool?)));

        public int? FadeAnimationDuration { get { return (int?)GetValue(FadeAnimationDurationProperty); } set { SetValue(FadeAnimationDurationProperty, value); } }
        public static readonly DependencyProperty FadeAnimationDurationProperty = DependencyProperty.Register(nameof(FadeAnimationDuration), typeof(int?), typeof(MvxCachedImageView), new PropertyMetadata(default(int?)));

        public bool? TransformPlaceholders { get { return (bool?)GetValue(TransformPlaceholdersProperty); } set { SetValue(TransformPlaceholdersProperty, value); } }
        public static readonly DependencyProperty TransformPlaceholdersProperty = DependencyProperty.Register(nameof(TransformPlaceholders), typeof(bool), typeof(MvxCachedImageView), new PropertyMetadata(default(bool?)));

        public CacheType? CacheType { get { return (CacheType?)GetValue(CacheTypeProperty); } set { SetValue(CacheTypeProperty, value); } }
        public static readonly DependencyProperty CacheTypeProperty = DependencyProperty.Register(nameof(CacheType), typeof(CacheType?), typeof(MvxCachedImageView), new PropertyMetadata(default(CacheType?)));

        public List<ITransformation> Transformations { get { return (List<ITransformation>)GetValue(TransformationsProperty); } set { SetValue(TransformationsProperty, value); } }
        public static readonly DependencyProperty TransformationsProperty = DependencyProperty.Register(nameof(Transformations), typeof(List<ITransformation>), typeof(MvxCachedImageView), new PropertyMetadata(new List<ITransformation>(), OnTransformationsChanged));

        public IDataResolver CustomDataResolver { get { return (IDataResolver)GetValue(CustomDataResolverProperty); } set { SetValue(CustomDataResolverProperty, value); } }
        public static readonly DependencyProperty CustomDataResolverProperty = DependencyProperty.Register(nameof(CustomDataResolver), typeof(IDataResolver), typeof(MvxCachedImageView), new PropertyMetadata(default(IDataResolver)));

        public IDataResolver CustomLoadingPlaceholderDataResolver { get { return (IDataResolver)GetValue(CustomLoadingPlaceholderDataResolverProperty); } set { SetValue(CustomLoadingPlaceholderDataResolverProperty, value); } }
        public static readonly DependencyProperty CustomLoadingPlaceholderDataResolverProperty = DependencyProperty.Register(nameof(CustomLoadingPlaceholderDataResolver), typeof(IDataResolver), typeof(MvxCachedImageView), new PropertyMetadata(default(IDataResolver)));

        public IDataResolver CustomErrorPlaceholderDataResolver { get { return (IDataResolver)GetValue(CustomErrorPlaceholderDataResolverProperty); } set { SetValue(CustomErrorPlaceholderDataResolverProperty, value); } }
        public static readonly DependencyProperty CustomErrorPlaceholderDataResolverProperty = DependencyProperty.Register(nameof(CustomErrorPlaceholderDataResolver), typeof(IDataResolver), typeof(MvxCachedImageView), new PropertyMetadata(default(IDataResolver)));

        public string LoadingPlaceholderImagePath { get { return (string)GetValue(LoadingPlaceholderImagePathProperty); } set { SetValue(LoadingPlaceholderImagePathProperty, value); } }
        public static readonly DependencyProperty LoadingPlaceholderImagePathProperty = DependencyProperty.Register(nameof(LoadingPlaceholderImagePath), typeof(string), typeof(MvxCachedImageView), new PropertyMetadata(default(string)));

        public string ErrorPlaceholderImagePath { get { return (string)GetValue(ErrorPlaceholderImagePathProperty); } set { SetValue(ErrorPlaceholderImagePathProperty, value); } }
        public static readonly DependencyProperty ErrorPlaceholderImagePathProperty = DependencyProperty.Register(nameof(ErrorPlaceholderImagePath), typeof(string), typeof(MvxCachedImageView), new PropertyMetadata(default(string)));

        public string ImagePath { get { return (string)GetValue(ImagePathProperty); } set { SetValue(ImagePathProperty, value); } }
        public static readonly DependencyProperty ImagePathProperty = DependencyProperty.Register(nameof(ImagePath), typeof(string), typeof(MvxCachedImageView), new PropertyMetadata(default(string), OnImageChanged));

        public Func<CancellationToken, Task<Stream>> ImageStream { get { return (Func<CancellationToken, Task<Stream>>)GetValue(ImageStreamProperty); } set { SetValue(ImageStreamProperty, value); } }
        public static readonly DependencyProperty ImageStreamProperty = DependencyProperty.Register(nameof(ImageStream), typeof(Func<CancellationToken, Task<Stream>>), typeof(MvxCachedImageView), new PropertyMetadata(default(Func<CancellationToken, Task<Stream>>), OnImageChanged));

        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(nameof(Stretch), typeof(Windows.UI.Xaml.Media.Stretch), typeof(MvxCachedImageView), new PropertyMetadata(default(Windows.UI.Xaml.Media.Stretch), StretchPropertyChanged));
        public Windows.UI.Xaml.Media.Stretch Stretch { get { return (Windows.UI.Xaml.Media.Stretch)GetValue(StretchProperty); } set { SetValue(StretchProperty, value); } }
        private static void StretchPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MvxCachedImageView)d)._internalImage.Stretch = (Windows.UI.Xaml.Media.Stretch)e.NewValue;
        }

        public static readonly DependencyProperty HorizontalImageAlignmentProperty = DependencyProperty.Register(nameof(HorizontalImageAlignment), typeof(HorizontalAlignment), typeof(MvxCachedImageView), new PropertyMetadata(HorizontalAlignment.Stretch, HorizontalImageAlignmentPropertyChanged));
        public HorizontalAlignment HorizontalImageAlignment { get { return (HorizontalAlignment)GetValue(HorizontalImageAlignmentProperty); } set { SetValue(HorizontalImageAlignmentProperty, value); } }
        private static void HorizontalImageAlignmentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MvxCachedImageView)d)._internalImage.HorizontalAlignment = ((HorizontalAlignment)e.NewValue);
        }

        public static readonly DependencyProperty VerticalImageAlignmentProperty = DependencyProperty.Register(nameof(VerticalImageAlignment), typeof(VerticalAlignment), typeof(MvxCachedImageView), new PropertyMetadata(VerticalAlignment.Stretch, VerticalImageAlignmentPropertyChanged));
        public VerticalAlignment VerticalImageAlignment { get { return (VerticalAlignment)GetValue(VerticalImageAlignmentProperty); } set { SetValue(VerticalImageAlignmentProperty, value); } }
        private static void VerticalImageAlignmentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MvxCachedImageView)d)._internalImage.VerticalAlignment = ((VerticalAlignment)e.NewValue);
        }

        public string CustomCacheKey { get; set; }

        public event EventHandler<SuccessEventArgs> OnSuccess;
        public event EventHandler<ErrorEventArgs> OnError;
        public event EventHandler<FinishEventArgs> OnFinish;
        public event EventHandler<DownloadStartedEventArgs> OnDownloadStarted;
        public event EventHandler<DownloadProgressEventArgs> OnDownloadProgress;
        public event EventHandler<FileWriteFinishedEventArgs> OnFileWriteFinished;

        public void Cancel()
        {
            try
            {
                var taskToCancel = _scheduledWork;
                if (taskToCancel != null && !taskToCancel.IsCancelled)
                {
                    taskToCancel.Cancel();
                }
            }
            catch (Exception) { }
        }

        public void Reload()
        {
            UpdateImageLoadingTask();
        }

        protected virtual void UpdateImageLoadingTask()
        {
            var ffSource = GetImageSourceBinding(ImagePath, ImageStream);
            var placeholderSource = GetImageSourceBinding(LoadingPlaceholderImagePath, null);

            Cancel();
            TaskParameter imageLoader = null;

            if (ffSource == null)
            {
                _internalImage.Source = null;
                IsLoading = false;
                return;
            }

            IsLoading = true;

            if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.Url)
            {
                imageLoader = ImageService.Instance.LoadUrl(ffSource.Path, CacheDuration);
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
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.Stream)
            {
                imageLoader = ImageService.Instance.LoadStream(ffSource.Stream);
            }
            else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.EmbeddedResource)
            {
                imageLoader = ImageService.Instance.LoadEmbeddedResource(ffSource.Path);
            }

            if (imageLoader != null)
            {
                // LoadingPlaceholder
                if (placeholderSource != null)
                {
                    if (placeholderSource != null)
                        imageLoader.LoadingPlaceholder(placeholderSource.Path, placeholderSource.ImageSource);
                }

                // ErrorPlaceholder
                if (!string.IsNullOrWhiteSpace(ErrorPlaceholderImagePath))
                {
                    var errorPlaceholderSource = GetImageSourceBinding(ErrorPlaceholderImagePath, null);
                    if (errorPlaceholderSource != null)
                        imageLoader.ErrorPlaceholder(errorPlaceholderSource.Path, errorPlaceholderSource.ImageSource);
                }

                if (CustomDataResolver != null)
                {
                    imageLoader.WithCustomDataResolver(CustomDataResolver);
                    imageLoader.WithCustomLoadingPlaceholderDataResolver(CustomLoadingPlaceholderDataResolver);
                    imageLoader.WithCustomErrorPlaceholderDataResolver(CustomErrorPlaceholderDataResolver);
                }

                // Downsample
                if ((int)DownsampleHeight != 0 || (int)DownsampleWidth != 0)
                {
					if (DownsampleUseDipUnits)
						imageLoader.DownSampleInDip((int)DownsampleWidth, (int)DownsampleHeight);
					else
						imageLoader.DownSample((int)DownsampleWidth, (int)DownsampleHeight);
				}

                // RetryCount
                if (RetryCount > 0)
                {
                    imageLoader.Retry(RetryCount, RetryDelay);
                }

                if (BitmapOptimizations.HasValue)
                    imageLoader.BitmapOptimizations(BitmapOptimizations.Value);

                // FadeAnimation
                if (FadeAnimationEnabled.HasValue)
                    imageLoader.FadeAnimation(FadeAnimationEnabled.Value, duration: FadeAnimationDuration);

                // FadeAnimationForCachedImages
                if (FadeAnimationEnabled.HasValue && FadeAnimationForCachedImages.HasValue)
                    imageLoader.FadeAnimation(FadeAnimationEnabled.Value, FadeAnimationForCachedImages.Value, FadeAnimationDuration);

                // TransformPlaceholders
                if (TransformPlaceholders.HasValue)
                    imageLoader.TransformPlaceholders(TransformPlaceholders.Value);

                // Transformations
                if (Transformations != null && Transformations.Count > 0)
                {
                    imageLoader.Transform(Transformations);
                }

                if (InvalidateLayoutAfterLoaded.HasValue)
                    imageLoader.InvalidateLayout(InvalidateLayoutAfterLoaded.Value);

                imageLoader.WithPriority(LoadingPriority);
                if (CacheType.HasValue)
                {
                    imageLoader.WithCache(CacheType.Value);
                }

                if (LoadingDelay > 0)
                {
                    imageLoader.Delay(LoadingDelay);
                }

                imageLoader.Finish((work) =>
                {
                    IsLoading = false;
                    OnFinish?.Invoke(this, new Args.FinishEventArgs(work));
                });

                imageLoader.Success((imageInformation, loadingResult) =>
                {
                    OnSuccess?.Invoke(this, new Args.SuccessEventArgs(imageInformation, loadingResult));
                    _lastImageSource = ffSource;
                });

                if (OnError != null)
                    imageLoader.Error((ex) => OnError?.Invoke(this, new Args.ErrorEventArgs(ex)));

                if (OnDownloadStarted != null)
                    imageLoader.DownloadStarted((downloadInformation) => OnDownloadStarted(this, new Args.DownloadStartedEventArgs(downloadInformation)));

                if (OnDownloadProgress != null)
                    imageLoader.DownloadProgress((progress) => OnDownloadProgress(this, new Args.DownloadProgressEventArgs(progress)));

                if (OnFileWriteFinished != null)
                    imageLoader.FileWriteFinished((info) => OnFileWriteFinished(this, new Args.FileWriteFinishedEventArgs(info)));

                if (!string.IsNullOrWhiteSpace(CustomCacheKey))
                    imageLoader.CacheKey(CustomCacheKey);

                SetupOnBeforeImageLoading(imageLoader);

                _scheduledWork = imageLoader.Into(_internalImage);
            }
        }

        /// <summary>
        /// Setups the on before image loading.
        /// You can add additional logic here to configure image loader settings before loading
        /// eg. custom cache keys, svg data resolvers, etc
        /// </summary>
        /// <param name="imageLoader">Image loader.</param>
        protected virtual void SetupOnBeforeImageLoading(Work.TaskParameter imageLoader)
        {
        }

        protected virtual ImageSourceBinding GetImageSourceBinding(string imagePath, Func<CancellationToken, Task<Stream>> imageStream)
        {
            if (string.IsNullOrWhiteSpace(imagePath) && imageStream == null)
                return null;

            if (imageStream != null)
                return new ImageSourceBinding(ImageSource.Stream, "Stream");

            if (imagePath.StartsWith("res:", StringComparison.OrdinalIgnoreCase))
            {
                var resourceName = imagePath.Split(new[] { "res:" }, StringSplitOptions.None)[1];
                return new ImageSourceBinding(ImageSource.CompiledResource, resourceName);
            }

            if (imagePath.IsDataUrl())
            {
                return new ImageSourceBinding(ImageSource.Url, imagePath);
            }

            Uri uri;
            if (Uri.TryCreate(imagePath, UriKind.Absolute, out uri))
            {
                if (uri.Scheme == "file")
                    return new ImageSourceBinding(ImageSource.Filepath, uri.LocalPath);

                if (uri.Scheme == "resource")
                    return new ImageSourceBinding(ImageSource.EmbeddedResource, imagePath);

                if (uri.Scheme == "app")
                    return new ImageSourceBinding(ImageSource.CompiledResource, uri.LocalPath);

                return new ImageSourceBinding(ImageSource.Url, imagePath);
            }

            return new ImageSourceBinding(ImageSource.CompiledResource, imagePath);
        }

        public virtual void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                Cancel();
            }
        }

        public class ImageSourceBinding
        {
            public ImageSourceBinding(ImageSource imageSource, string path)
            {
                ImageSource = imageSource;
                Path = path;
            }

            public ImageSourceBinding(Func<CancellationToken, Task<Stream>> stream)
            {
                ImageSource = ImageSource.Stream;
                Stream = stream;
                Path = "Stream";
            }

            public ImageSource ImageSource { get; private set; }

            public string Path { get; private set; }

            public Func<CancellationToken, Task<Stream>> Stream { get; private set; }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + this.ImageSource.GetHashCode();
                    hash = hash * 23 + Path.GetHashCode();
                    hash = hash * 23 + Stream.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
