using System;
using System.Collections.Generic;
using FFImageLoading.Work;
using FFImageLoading.Cache;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using FFImageLoading.Views;

#if __IOS__
using Foundation;
using UIKit;
using CoreGraphics;
#elif __ANDROID__
using Android.Util;
using Android.Runtime;
using Android.Content;
#endif

namespace FFImageLoading.Cross
{

#if __IOS__
    [Preserve(AllMembers = true)]
    [Register("MvxCachedImageView")]
#elif __ANDROID__
    [Preserve(AllMembers = true)]
    [Register("ffimageloading.cross.MvxCachedImageView")]
#endif
    /// <summary>
    /// MvxCachedImageView by Daniel Luberda
    /// </summary>
    public class MvxCachedImageView
#if __IOS__
        : UIImageView, ICachedImageView, INotifyPropertyChanged
#elif __ANDROID__
        : Android.Widget.ImageView, ICachedImageView, INotifyPropertyChanged
#endif
    {
#if __IOS__
        /// <summary>
        /// MvxCachedImageView by Daniel Luberda
        /// </summary>
        public MvxCachedImageView() { Initialize(); }
        public MvxCachedImageView(IntPtr handle) : base(handle) { Initialize(); }
        public MvxCachedImageView(CGRect frame) : base(frame) { Initialize(); }
#elif __ANDROID__
        /// <summary>
        /// MvxCachedImageView by Daniel Luberda
        /// </summary>
        public MvxCachedImageView(Context context) : base(context) { Initialize(); }
        public MvxCachedImageView(Context context, IAttributeSet attrs) : base(context, attrs) { Initialize(); }
        public MvxCachedImageView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { Initialize(); }
#endif

        protected IScheduledWork _scheduledWork;
        protected ImageSourceBinding _lastImageSource;
        protected bool _isDisposed;

        protected void Initialize()
        {
            Transformations = new List<ITransformation>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName == nameof(ImagePath))
            {
                UpdateImageLoadingTask();
            }
            else if (propertyName == nameof(ImageStream))
            {
                UpdateImageLoadingTask();
            }
            else if (propertyName == nameof(Transformations))
            {
                if (_lastImageSource != null)
                {
                    UpdateImageLoadingTask();
                }
            }
        }

        public event EventHandler IsLoadingChanged;

        bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { if (_isLoading != value) { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); IsLoadingChanged?.Invoke(this, EventArgs.Empty); } }
        }

        int _retryCount = 3;
        public int RetryCount
        {
            get { return _retryCount; }
            set { if (_retryCount != value) { _retryCount = value; OnPropertyChanged(nameof(RetryCount)); } }
        }

        int _retryDelay = 500;
        public int RetryDelay
        {
            get { return _retryDelay; }
            set { if (_retryDelay != value) { _retryDelay = value; OnPropertyChanged(nameof(RetryDelay)); } }
        }

        int _loadingDelay;
        public int LoadingDelay
        {
            get { return _loadingDelay; }
            set { if (_loadingDelay != value) { _loadingDelay = value; OnPropertyChanged(nameof(LoadingDelay)); } }
        }

        double _downsampleWidth;
        public double DownsampleWidth
        {
            get { return _downsampleWidth; }
            set { if (Math.Abs(_downsampleWidth - value) > double.Epsilon) { _downsampleWidth = value; OnPropertyChanged(nameof(DownsampleWidth)); } }
        }

        double _downsampleHeight;
        public double DownsampleHeight
        {
            get { return _downsampleHeight; }
            set { if (Math.Abs(_downsampleHeight - value) > double.Epsilon) { _downsampleHeight = value; OnPropertyChanged(nameof(DownsampleHeight)); } }
        }

        bool _downsampleUseDipUnits;
        public bool DownsampleUseDipUnits
        {
            get { return _downsampleUseDipUnits; }
            set { if (_downsampleUseDipUnits != value) { _downsampleUseDipUnits = value; OnPropertyChanged(nameof(DownsampleUseDipUnits)); } }
        }

        TimeSpan? _cacheDuration;
        public TimeSpan? CacheDuration
        {
            get { return _cacheDuration; }
            set { if (_cacheDuration != value) { _cacheDuration = value; OnPropertyChanged(nameof(CacheDuration)); } }
        }

        LoadingPriority _loadingPriority;
        public LoadingPriority LoadingPriority
        {
            get { return _loadingPriority; }
            set { if (_loadingPriority != value) { _loadingPriority = value; OnPropertyChanged(nameof(LoadingPriority)); } }
        }

        bool? _bitmapOptimizations;
        public bool? BitmapOptimizations
        {
            get { return _bitmapOptimizations; }
            set { if (_bitmapOptimizations != value) { _bitmapOptimizations = value; OnPropertyChanged(nameof(BitmapOptimizations)); } }
        }

        bool? _fadeAnimationEnabled;
        public bool? FadeAnimationEnabled
        {
            get { return _fadeAnimationEnabled; }
            set { if (_fadeAnimationEnabled != value) { _fadeAnimationEnabled = value; OnPropertyChanged(nameof(FadeAnimationEnabled)); } }
        }

        bool? _fadeAnimationForCachedImages;
        public bool? FadeAnimationForCachedImages
        {
            get { return _fadeAnimationForCachedImages; }
            set { if (_fadeAnimationForCachedImages != value) { _fadeAnimationForCachedImages = value; OnPropertyChanged(nameof(FadeAnimationForCachedImages)); } }
        }

        int? _fadeAnimationDuration;
        public int? FadeAnimationDuration
        {
            get { return _fadeAnimationDuration; }
            set { if (_fadeAnimationDuration != value) { _fadeAnimationDuration = value; OnPropertyChanged(nameof(FadeAnimationDuration)); } }
        }

        bool? _transformPlaceholders;
        public bool? TransformPlaceholders
        {
            get { return _transformPlaceholders; }
            set { if (_transformPlaceholders != value) { _transformPlaceholders = value; OnPropertyChanged(nameof(TransformPlaceholders)); } }
        }

        CacheType? _cacheType;
        public CacheType? CacheType
        {
            get { return _cacheType; }
            set { if (_cacheType != value) { _cacheType = value; OnPropertyChanged(nameof(CacheType)); } }
        }

        List<ITransformation> _transformations;
        public List<ITransformation> Transformations
        {
            get { return _transformations; }
            set { if (_transformations != value) { _transformations = value; OnPropertyChanged(nameof(Transformations)); } }
        }

        bool? _invalidateLayoutAfterLoaded;
        public bool? InvalidateLayoutAfterLoaded
        {
            get { return _invalidateLayoutAfterLoaded; }
            set { if (_invalidateLayoutAfterLoaded != value) { _invalidateLayoutAfterLoaded = value; OnPropertyChanged(nameof(InvalidateLayoutAfterLoaded)); } }
        }

        IDataResolver _customDataResolver;
        public IDataResolver CustomDataResolver
        {
            get { return _customDataResolver; }
            set { if (_customDataResolver != value) { _customDataResolver = value; OnPropertyChanged(nameof(CustomDataResolver)); } }
        }

        IDataResolver _customLoadingPlaceholderDataResolver;
        public IDataResolver CustomLoadingPlaceholderDataResolver
        {
            get { return _customLoadingPlaceholderDataResolver; }
            set { if (_customLoadingPlaceholderDataResolver != value) { _customLoadingPlaceholderDataResolver = value; OnPropertyChanged(nameof(CustomLoadingPlaceholderDataResolver)); } }
        }

        IDataResolver _customErrorPlaceholderDataResolver;
        public IDataResolver CustomErrorPlaceholderDataResolver
        {
            get { return _customErrorPlaceholderDataResolver; }
            set { if (_customErrorPlaceholderDataResolver != value) { _customErrorPlaceholderDataResolver = value; OnPropertyChanged(nameof(CustomErrorPlaceholderDataResolver)); } }
        }

        public event EventHandler<Args.SuccessEventArgs> OnSuccess;

        public event EventHandler<Args.ErrorEventArgs> OnError;

        public event EventHandler<Args.FinishEventArgs> OnFinish;

        public event EventHandler<Args.DownloadStartedEventArgs> OnDownloadStarted;

        public event EventHandler<Args.DownloadProgressEventArgs> OnDownloadProgress;

        public event EventHandler<Args.FileWriteFinishedEventArgs> OnFileWriteFinished;

        string _loadingPlaceholderPath;
        public string LoadingPlaceholderImagePath
        {
            get { return _loadingPlaceholderPath; }
            set { if (_loadingPlaceholderPath != value) { _loadingPlaceholderPath = value; OnPropertyChanged(nameof(LoadingPlaceholderImagePath)); } }
        }

        string _errorPlaceholderPath;
        public string ErrorPlaceholderImagePath
        {
            get { return _errorPlaceholderPath; }
            set { if (_errorPlaceholderPath != value) { _errorPlaceholderPath = value; OnPropertyChanged(nameof(ErrorPlaceholderImagePath)); } }
        }

        string _imagePath;
        public string ImagePath
        {
            get { return _imagePath; }
            set { if (_imagePath != value) { _imagePath = value; OnPropertyChanged(nameof(ImagePath)); } }
        }

        string _customCacheKey;
        public string CustomCacheKey
        {
            get { return _customCacheKey; }
            set { if (_customCacheKey != value) { _customCacheKey = value; OnPropertyChanged(nameof(CustomCacheKey)); } }
        }

        Func<CancellationToken, Task<Stream>> _imageStream;
        public Func<CancellationToken, Task<Stream>> ImageStream
        {
            get { return _imageStream; }
            set { if (_imageStream != value) { _imageStream = value; OnPropertyChanged(nameof(ImageStream)); } }
        }

        public void Cancel()
        {
            try
            {
                _scheduledWork?.Cancel();
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

	        Cancel();

	        if (ffSource == null)
	        {
#if __ANDROID__
				this.SetImageResource(global::Android.Resource.Color.Transparent);
#elif __IOS__
                Image = null;
#endif
		        IsLoading = false;
		        return;
	        }

	        IsLoading = true;

	        var imageLoader = GetImageLoaderForSource(ffSource);
	        if (imageLoader != null)
	        {
		        ConfigureImageLoader(imageLoader, ffSource);
		        SetupOnBeforeImageLoading(imageLoader);

		        _scheduledWork = imageLoader.Into(this);
	        }
        }

        protected virtual TaskParameter GetImageLoaderForSource(ImageSourceBinding ffSource)
        {
	        switch (ffSource.ImageSource)
	        {
		        case FFImageLoading.Work.ImageSource.Url:
			        return ImageService.Instance.LoadUrl(ffSource.Path, CacheDuration);
		        case FFImageLoading.Work.ImageSource.CompiledResource:
			        return ImageService.Instance.LoadCompiledResource(ffSource.Path);
		        case FFImageLoading.Work.ImageSource.ApplicationBundle:
			        return ImageService.Instance.LoadFileFromApplicationBundle(ffSource.Path);
		        case FFImageLoading.Work.ImageSource.Filepath:
			        return ImageService.Instance.LoadFile(ffSource.Path);
		        case FFImageLoading.Work.ImageSource.Stream:
			        return ImageService.Instance.LoadStream(ffSource.Stream);
		        case FFImageLoading.Work.ImageSource.EmbeddedResource:
			        return ImageService.Instance.LoadEmbeddedResource(ffSource.Path);
		        default:
			        return null;
	        }
        }

        protected virtual void ConfigureImageLoader(TaskParameter imageLoader, ImageSourceBinding ffSource)
		{
			var placeholderSource = GetImageSourceBinding(LoadingPlaceholderImagePath, null);

			// LoadingPlaceholder
			if (placeholderSource != null)
			{
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
			}

			if (CustomLoadingPlaceholderDataResolver != null)
			{
				imageLoader.WithCustomLoadingPlaceholderDataResolver(CustomLoadingPlaceholderDataResolver);
			}

			if (CustomErrorPlaceholderDataResolver != null)
			{
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
		}

        /// <summary>
        /// Setups the on before image loading.
        /// You can add additional logic here to configure image loader settings before loading
        /// eg. custom cache keys, svg data resolvers, etc
        /// </summary>
        /// <param name="imageLoader">Image loader.</param>
        protected virtual void SetupOnBeforeImageLoading(TaskParameter imageLoader)
        {
        }

        protected virtual ImageSourceBinding GetImageSourceBinding(string imagePath, Func<CancellationToken, Task<Stream>> imageStream)
        {
            if (string.IsNullOrWhiteSpace(imagePath) && imageStream == null)
                return null;

            if (imageStream != null)
                return new ImageSourceBinding(imageStream);

            if (imagePath.StartsWith("res:", StringComparison.OrdinalIgnoreCase))
            {
                var resourceName = imagePath.Split(new[] { "res:" }, StringSplitOptions.None)[1];
                return new ImageSourceBinding(ImageSource.CompiledResource, resourceName);
            }

#if __ANDROID__
            if (imagePath.StartsWith("android.resource", StringComparison.OrdinalIgnoreCase))
            {
                var substrings = imagePath.Split(new[] { "/" }, StringSplitOptions.None);
                var resourceName = Context.Resources.GetResourceEntryName(Convert.ToInt32(substrings[substrings.Length - 1]));
                return new ImageSourceBinding(ImageSource.CompiledResource, resourceName);
            }
#endif

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
                    return new ImageSourceBinding(ImageSource.EmbeddedResource,imagePath);

                if (uri.Scheme == "app")
                    return new ImageSourceBinding(ImageSource.CompiledResource, uri.LocalPath);

                return new ImageSourceBinding(ImageSource.Url, imagePath);
            }

            if (!string.IsNullOrWhiteSpace(Path.GetDirectoryName(imagePath)) && File.Exists(imagePath))
                return new ImageSourceBinding(ImageSource.Filepath, imagePath);

            return new ImageSourceBinding(ImageSource.CompiledResource, imagePath);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                Cancel();
            }

            base.Dispose(disposing);
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
