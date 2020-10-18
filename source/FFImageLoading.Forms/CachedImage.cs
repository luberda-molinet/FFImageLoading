using System;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using FFImageLoading.Forms.Args;
using System.Windows.Input;
using FFImageLoading.Cache;
using System.Reflection;

namespace FFImageLoading.Forms
{
	[Preserve(AllMembers = true)]
	[RenderWith(typeof(Platform.CachedImageRenderer._CachedImageRenderer))]
	/// <summary>
	/// CachedImage by Daniel Luberda
	/// </summary>
	public class CachedImage : View
	{
		private static bool? _isDesignModeEnabled = null;
		protected static bool IsDesignModeEnabled
		{
			get
			{
				// works only on Xamarin.Forms >= 3.0
				if (!_isDesignModeEnabled.HasValue)
				{
					var type = typeof(Image).GetTypeInfo().Assembly.GetType("Xamarin.Forms.DesignMode");
					if (type == null)
					{
						_isDesignModeEnabled = true;
					}
					else
					{
						var property = type.GetRuntimeProperty("IsDesignModeEnabled");
						_isDesignModeEnabled = (bool)property.GetValue(null);
					}
				}

				return _isDesignModeEnabled.Value;
			}
		}

		private static readonly PropertyInfo _visualMarkerProperty = typeof(VisualElement).GetTypeInfo().Assembly.GetType("Xamarin.Forms.VisualMarker")?.GetRuntimeProperty("Default");
		private static readonly PropertyInfo _visualProperty = typeof(VisualElement).GetRuntimeProperty("Visual");

		internal static bool IsRendererInitialized { get; set; } = IsDesignModeEnabled;

		[Obsolete]
		public static bool FixedOnMeasureBehavior { get; set; } = true;
		[Obsolete]
		public static bool FixedAndroidMotionEventHandler { get; set; } = true;

		private bool _reloadBecauseOfMissingSize;

		/// <summary>
		/// CachedImage by Daniel Luberda
		/// </summary>
		public CachedImage()
		{
			Transformations = new List<Work.ITransformation>();

			// Fix for issues with non-default visual style
			if (_visualProperty != null && _visualMarkerProperty != null)
			{
				_visualProperty.SetValue(this, _visualMarkerProperty.GetValue(null));
			}
		}

		/// <summary>
		/// The aspect property.
		/// </summary>
		public static readonly BindableProperty AspectProperty = BindableProperty.Create(nameof(Aspect), typeof(Aspect), typeof(CachedImage), Aspect.AspectFit);

		/// <summary>
		/// Gets or sets the aspect.
		/// </summary>
		/// <value>The aspect.</value>
		public Aspect Aspect
		{
			get => (Aspect)GetValue(AspectProperty);
			set => SetValue(AspectProperty, value);
		}

		/// <summary>
		/// The is loading property key.
		/// </summary>
		public static readonly BindablePropertyKey IsLoadingPropertyKey = BindableProperty.CreateReadOnly(nameof(IsLoading), typeof(bool), typeof(CachedImage), false);

		/// <summary>
		/// The is loading property.
		/// </summary>
		public static readonly BindableProperty IsLoadingProperty = CachedImage.IsLoadingPropertyKey.BindableProperty;

		/// <summary>
		/// Gets a value indicating whether this instance is loading.
		/// </summary>
		/// <value><c>true</c> if this instance is loading; otherwise, <c>false</c>.</value>
		public bool IsLoading => (bool)GetValue(IsLoadingProperty);

		/// <summary>
		/// The is opaque property.
		/// </summary>
		public static readonly BindableProperty IsOpaqueProperty = BindableProperty.Create(nameof(IsOpaque), typeof(bool), typeof(CachedImage), false);

		/// <summary>
		/// Gets or sets a value indicating whether this instance is opaque.
		/// </summary>
		/// <value><c>true</c> if this instance is opaque; otherwise, <c>false</c>.</value>
		public bool IsOpaque
		{
			get => (bool)GetValue(IsOpaqueProperty);
			set => SetValue(IsOpaqueProperty, value);
		}

		/// <summary>
		/// The source property.
		/// </summary>
		public static readonly BindableProperty SourceProperty = BindableProperty.Create(nameof(Source), typeof(ImageSource), typeof(CachedImage), default(ImageSource), BindingMode.OneWay, coerceValue: CoerceImageSource, propertyChanged: OnSourcePropertyChanged);

		private static object CoerceImageSource(BindableObject bindable, object newValue)
		{
			return ((CachedImage)bindable).CoerceImageSource(newValue);
		}

		private static void OnSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (!IsRendererInitialized)
			{
				if (ImageService.EnableMockImageService)
					return;

				throw new Exception("Please call CachedImageRenderer.Init method in a platform specific project to use FFImageLoading!");
			}

			if (newValue != null)
			{
				SetInheritedBindingContext(newValue as BindableObject, bindable.BindingContext);
			}
		}

		protected virtual ImageSource CoerceImageSource(object newValue)
		{
			var uriImageSource = newValue as UriImageSource;

			if (uriImageSource?.Uri?.OriginalString != null)
			{
				if (uriImageSource.Uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
					return ImageSource.FromFile(uriImageSource.Uri.LocalPath);

				if (uriImageSource.Uri.Scheme.Equals("resource", StringComparison.OrdinalIgnoreCase))
					return new EmbeddedResourceImageSource(uriImageSource.Uri);

				if (uriImageSource.Uri.OriginalString.IsDataUrl())
					return new DataUrlImageSource(uriImageSource.Uri.OriginalString);
			}

			return newValue as ImageSource;
		}

		/// <summary>
		/// Gets or sets the source.
		/// </summary>
		/// <value>The source.</value>
		[TypeConverter(typeof(ImageSourceConverter))]
		public ImageSource Source
		{
			get => (ImageSource)GetValue(SourceProperty);
			set => SetValue(SourceProperty, value);
		}

		/// <summary>
		/// The retry count property.
		/// </summary>
		public static readonly BindableProperty RetryCountProperty = BindableProperty.Create(nameof(RetryCount), typeof(int), typeof(CachedImage), 3);

		/// <summary>
		/// If image loading fails automatically retry it a number of times, with a specific delay. Sets number of retries.
		/// </summary>
		public int RetryCount
		{
			get => (int)GetValue(RetryCountProperty);
			set => SetValue(RetryCountProperty, value);
		}

		/// <summary>
		/// The retry delay property.
		/// </summary>
		public static readonly BindableProperty RetryDelayProperty = BindableProperty.Create(nameof(RetryDelay), typeof(int), typeof(CachedImage), 250);

		/// <summary>
		/// If image loading fails automatically retry it a number of times, with a specific delay. Sets delay in milliseconds between each trial
		/// </summary>
		public int RetryDelay
		{
			get => (int)GetValue(RetryDelayProperty);
			set => SetValue(RetryDelayProperty, value);
		}

		/// <summary>
		/// The loading delay property.
		/// </summary>
		public static readonly BindableProperty LoadingDelayProperty = BindableProperty.Create(nameof(LoadingDelay), typeof(int?), typeof(CachedImage), default(int?));

		/// <summary>
		/// Sets delay in milliseconds before image loading
		/// </summary>
		public int? LoadingDelay
		{
			get => (int?)GetValue(LoadingDelayProperty);
			set => SetValue(LoadingDelayProperty, value);
		}

		/// <summary>
		/// The downsample width property.
		/// </summary>
		public static readonly BindableProperty DownsampleWidthProperty = BindableProperty.Create(nameof(DownsampleWidth), typeof(double), typeof(CachedImage), 0d);

		/// <summary>
		/// Reduce memory usage by downsampling the image. Aspect ratio will be kept even if width/height values are incorrect.
		/// Optional DownsampleWidth parameter, if value is higher than zero it will try to downsample to this width while keeping aspect ratio.
		/// </summary>
		public double DownsampleWidth
		{
			get => (double)GetValue(DownsampleWidthProperty);
			set => SetValue(DownsampleWidthProperty, value);
		}

		/// <summary>
		/// The downsample height property.
		/// </summary>
		public static readonly BindableProperty DownsampleHeightProperty = BindableProperty.Create(nameof(DownsampleHeight), typeof(double), typeof(CachedImage), 0d);

		/// <summary>
		/// Reduce memory usage by downsampling the image. Aspect ratio will be kept even if width/height values are incorrect.
		/// Optional DownsampleHeight parameter, if value is higher than zero it will try to downsample to this height while keeping aspect ratio.
		/// </summary>
		public double DownsampleHeight
		{
			get => (double)GetValue(DownsampleHeightProperty);
			set => SetValue(DownsampleHeightProperty, value);
		}

		/// <summary>
		/// The downsample to view size property.
		/// </summary>
		public static readonly BindableProperty DownsampleToViewSizeProperty = BindableProperty.Create(nameof(DownsampleToViewSize), typeof(bool), typeof(CachedImage), false);

		/// <summary>
		/// Reduce memory usage by downsampling the image. Aspect ratio will be kept even if width/height values are incorrect.
		/// DownsampleWidth and DownsampleHeight properties will be automatically set to view size
		/// </summary>
		/// <value><c>true</c> if downsample to view size; otherwise, <c>false</c>.</value>
		public bool DownsampleToViewSize
		{
			get => (bool)GetValue(DownsampleToViewSizeProperty);
			set => SetValue(DownsampleToViewSizeProperty, value);
		}

		/// <summary>
		/// The downsample use dip units property.
		/// </summary>
		public static readonly BindableProperty DownsampleUseDipUnitsProperty = BindableProperty.Create(nameof(DownsampleUseDipUnits), typeof(bool), typeof(CachedImage), false);

		/// <summary>
		/// If set to <c>true</c> DownsampleWidth and DownsampleHeight properties
		/// will use density independent pixels for downsampling
		/// </summary>
		/// <value><c>true</c> if downsample use dip units; otherwise, <c>false</c>.</value>
		public bool DownsampleUseDipUnits
		{
			get => (bool)GetValue(DownsampleUseDipUnitsProperty);
			set => SetValue(DownsampleUseDipUnitsProperty, value);
		}

		/// <summary>
		/// The cache duration property.
		/// </summary>
		public static readonly BindableProperty CacheDurationProperty = BindableProperty.Create(nameof(CacheDuration), typeof(TimeSpan?), typeof(CachedImage));

		/// <summary>
		/// How long the file will be cached on disk.
		/// </summary>
		public TimeSpan? CacheDuration
		{
			get => (TimeSpan?)GetValue(CacheDurationProperty);
			set => SetValue(CacheDurationProperty, value);
		}

		/// <summary>
		/// The loading priority property.
		/// </summary>
		public static readonly BindableProperty LoadingPriorityProperty = BindableProperty.Create(nameof(LoadingPriority), typeof(Work.LoadingPriority), typeof(CachedImage), Work.LoadingPriority.Normal);

		/// <summary>
		/// Defines the loading priority, the default is LoadingPriority.Normal
		/// </summary>
		public Work.LoadingPriority LoadingPriority
		{
			get => (Work.LoadingPriority)GetValue(LoadingPriorityProperty);
			set => SetValue(LoadingPriorityProperty, value);
		}

		/// <summary>
		/// The bitmap optimizations property.
		/// </summary>
		public static readonly BindableProperty BitmapOptimizationsProperty = BindableProperty.Create(nameof(BitmapOptimizations), typeof(bool?), typeof(CachedImage), default(bool?));

		/// <summary>
		/// Enables or disables the bitmap optimizations.
		/// </summary>
		/// <value>The bitmap optimizations.</value>
		public bool? BitmapOptimizations
		{
			get => (bool?)GetValue(BitmapOptimizationsProperty);
			set => SetValue(BitmapOptimizationsProperty, value);
		}

		/// <summary>
		/// The fade animation for cached images enabled property.
		/// </summary>
		public static readonly BindableProperty FadeAnimationForCachedImagesProperty = BindableProperty.Create(nameof(FadeAnimationForCachedImages), typeof(bool?), typeof(CachedImage), default(bool?));

		/// <summary>
		/// Indicates if the fade animation effect for cached images should be enabled. By default this value comes from ImageService.Instance.Config.FadeAnimationForCachedImages.
		/// </summary>
		public bool? FadeAnimationForCachedImages
		{
			get => (bool?)GetValue(FadeAnimationForCachedImagesProperty);
			set => SetValue(FadeAnimationForCachedImagesProperty, value);
		}

		/// <summary>
		/// The fade animation enabled property.
		/// </summary>
		public static readonly BindableProperty FadeAnimationEnabledProperty = BindableProperty.Create(nameof(FadeAnimationEnabled), typeof(bool?), typeof(CachedImage), default(bool?));

		/// <summary>
		/// Indicates if the fade animation effect should be enabled. By default this value comes from ImageService.Instance.Config.FadeAnimationEnabled.
		/// </summary>
		public bool? FadeAnimationEnabled
		{
			get => (bool?)GetValue(FadeAnimationEnabledProperty);
			set => SetValue(FadeAnimationEnabledProperty, value);
		}

		/// <summary>
		/// The fade animation duration property.
		/// </summary>
		public static readonly BindableProperty FadeAnimationDurationProperty = BindableProperty.Create(nameof(FadeAnimationDuration), typeof(int?), typeof(CachedImage), default(int?));

		/// <summary>
		/// Sets fade animation effect duration. By default this value comes from ImageService.Instance.Config.FadeAnimationDuration.
		/// </summary>
		public int? FadeAnimationDuration
		{
			get => (int?)GetValue(FadeAnimationDurationProperty);
			set => SetValue(FadeAnimationDurationProperty, value);
		}

		/// <summary>
		/// The loading placeholder property.
		/// </summary>
		public static readonly BindableProperty LoadingPlaceholderProperty = BindableProperty.Create(nameof(LoadingPlaceholder), typeof(ImageSource), typeof(CachedImage), default(ImageSource), coerceValue: CoerceImageSource);

		/// <summary>
		/// Gets or sets the loading placeholder image.
		/// </summary>
		[TypeConverter(typeof(ImageSourceConverter))]
		public ImageSource LoadingPlaceholder
		{
			get => (ImageSource)GetValue(LoadingPlaceholderProperty);
			set => SetValue(LoadingPlaceholderProperty, value);
		}

		/// <summary>
		/// The error placeholder property.
		/// </summary>
		public static readonly BindableProperty ErrorPlaceholderProperty = BindableProperty.Create(nameof(ErrorPlaceholder), typeof(ImageSource), typeof(CachedImage), default(ImageSource), coerceValue: CoerceImageSource);

		/// <summary>
		/// Gets or sets the error placeholder image.
		/// </summary>
		[TypeConverter(typeof(ImageSourceConverter))]
		public ImageSource ErrorPlaceholder
		{
			get => (ImageSource)GetValue(ErrorPlaceholderProperty);
			set => SetValue(ErrorPlaceholderProperty, value);
		}

		/// <summary>
		/// The TransformPlaceholders property.
		/// </summary>
		public static readonly BindableProperty TransformPlaceholdersProperty = BindableProperty.Create(nameof(TransformPlaceholders), typeof(bool?), typeof(CachedImage), default(bool?));

		/// <summary>
		/// Indicates if transforms should be applied to placeholders. By default this value comes from ImageService.Instance.Config.TransformPlaceholders.
		/// </summary>
		/// <value>The transform placeholders.</value>
		public bool? TransformPlaceholders
		{
			get => (bool?)GetValue(TransformPlaceholdersProperty);
			set => SetValue(TransformPlaceholdersProperty, value);
		}

		/// <summary>
		/// The transformations property.
		/// </summary>
		public static readonly BindableProperty TransformationsProperty = BindableProperty.Create(nameof(Transformations), typeof(List<Work.ITransformation>), typeof(CachedImage), new List<Work.ITransformation>(), propertyChanged: HandleTransformationsPropertyChangedDelegate);

		/// <summary>
		/// Gets or sets the transformations.
		/// </summary>
		/// <value>The transformations.</value>
		public List<Work.ITransformation> Transformations
		{
			get => (List<Work.ITransformation>)GetValue(TransformationsProperty);
			set => SetValue(TransformationsProperty, value);
		}

		/// <summary>
		/// The invalidate layout after loaded property.
		/// </summary>
		public static readonly BindableProperty InvalidateLayoutAfterLoadedProperty = BindableProperty.Create(nameof(InvalidateLayoutAfterLoaded), typeof(bool?), typeof(CachedImage), default(bool?));

		/// <summary>
		/// Specifies if view layout should be invalidated after image is loaded.
		/// </summary>
		/// <value>The invalidate layout after loaded.</value>
		public bool? InvalidateLayoutAfterLoaded
		{
			get => (bool?)GetValue(InvalidateLayoutAfterLoadedProperty);
			set => SetValue(InvalidateLayoutAfterLoadedProperty, value);
		}

		private static void HandleTransformationsPropertyChangedDelegate(BindableObject bindable, object oldValue, object newValue)
		{
			if (oldValue != newValue)
			{
				if (bindable is CachedImage cachedImage && cachedImage?.Source != null)
				{
					cachedImage.ReloadImage();
				}
			}
		}

		/// <summary>
		/// Gets or sets the cache custom key factory.
		/// </summary>
		/// <value>The cache key factory.</value>
		public ICacheKeyFactory CacheKeyFactory { get; set; }

		/// <summary>
		/// Gets or sets the custom data resolver for eg. SVG support (another nuget)
		/// </summary>
		/// <value>The custom data resolver.</value>
		public Work.IDataResolver CustomDataResolver { get; set; }

		//
		// Methods
		//
		protected override void OnBindingContextChanged()
		{
			if (Source != null)
			{
				SetInheritedBindingContext(Source, BindingContext);
			}

			base.OnBindingContextChanged();
		}

		protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
		{
			var desiredSize = base.OnMeasure(double.PositiveInfinity, double.PositiveInfinity);
			var desiredWidth = double.IsNaN(desiredSize.Request.Width) ? 0 : desiredSize.Request.Width;
			var desiredHeight = double.IsNaN(desiredSize.Request.Height) ? 0 : desiredSize.Request.Height;

			if (double.IsNaN(widthConstraint))
				widthConstraint = 0;
			if (double.IsNaN(heightConstraint))
				heightConstraint = 0;

			if (Math.Abs(desiredWidth) < double.Epsilon || Math.Abs(desiredHeight) < double.Epsilon)
				return new SizeRequest(new Size(0, 0));

			if (FixedOnMeasureBehavior)
			{
				var desiredAspect = desiredSize.Request.Width / desiredSize.Request.Height;
				var constraintAspect = widthConstraint / heightConstraint;
				var width = desiredWidth;
				var height = desiredHeight;

				if (constraintAspect > desiredAspect)
				{
					// constraint area is proportionally wider than image
					switch (Aspect)
					{
						case Aspect.AspectFit:
						case Aspect.AspectFill:
							height = Math.Min(desiredHeight, heightConstraint);
							width = desiredWidth * (height / desiredHeight);
							break;
						case Aspect.Fill:
							width = Math.Min(desiredWidth, widthConstraint);
							height = desiredHeight * (width / desiredWidth);
							break;
					}
				}
				else if (constraintAspect < desiredAspect)
				{
					// constraint area is proportionally taller than image
					switch (Aspect)
					{
						case Aspect.AspectFit:
						case Aspect.AspectFill:
							width = Math.Min(desiredWidth, widthConstraint);
							height = desiredHeight * (width / desiredWidth);
							break;
						case Aspect.Fill:
							height = Math.Min(desiredHeight, heightConstraint);
							width = desiredWidth * (height / desiredHeight);
							break;
					}
				}
				else
				{
					// constraint area is same aspect as image
					width = Math.Min(desiredWidth, widthConstraint);
					height = desiredHeight * (width / desiredWidth);
				}

				return new SizeRequest(new Size(double.IsNaN(width) ? 0 : width, double.IsNaN(height) ? 0 : height));
			}

			if (double.IsPositiveInfinity(widthConstraint) && double.IsPositiveInfinity(heightConstraint))
			{
				return new SizeRequest(new Size(desiredWidth, desiredHeight));
			}

			if (double.IsPositiveInfinity(widthConstraint))
			{
				var factor = heightConstraint / desiredHeight;
				return new SizeRequest(new Size(desiredWidth * factor, desiredHeight * factor));
			}

			if (double.IsPositiveInfinity(heightConstraint))
			{
				var factor = widthConstraint / desiredWidth;
				return new SizeRequest(new Size(desiredWidth * factor, desiredHeight * factor));
			}

			var fitsWidthRatio = widthConstraint / desiredWidth;
			var fitsHeightRatio = heightConstraint / desiredHeight;

			if (double.IsNaN(fitsWidthRatio))
				fitsWidthRatio = 0;
			if (double.IsNaN(fitsHeightRatio))
				fitsHeightRatio = 0;

			if (Math.Abs(fitsWidthRatio) < double.Epsilon && Math.Abs(fitsHeightRatio) < double.Epsilon)
				return new SizeRequest(new Size(0, 0));

			if (Math.Abs(fitsWidthRatio) < double.Epsilon)
				return new SizeRequest(new Size(desiredWidth * fitsHeightRatio, desiredHeight * fitsHeightRatio));

			if (Math.Abs(fitsHeightRatio) < double.Epsilon)
				return new SizeRequest(new Size(desiredWidth * fitsWidthRatio, desiredHeight * fitsWidthRatio));

			var ratioFactor = Math.Min(fitsWidthRatio, fitsHeightRatio);

			return new SizeRequest(new Size(desiredWidth * ratioFactor, desiredHeight * ratioFactor));
		}

		public void SetIsLoading(bool isLoading)
		{
			SetValue(IsLoadingPropertyKey, isLoading);
		}

		internal Action InternalReloadImage;

		/// <summary>
		/// Reloads the image.
		/// </summary>
		public void ReloadImage() => InternalReloadImage?.Invoke();

		internal Action InternalCancel;
		/// <summary>
		/// Cancels image loading tasks
		/// </summary>
		public void Cancel() => InternalCancel?.Invoke();

		internal Func<GetImageAsJpgArgs, Task<byte[]>> InternalGetImageAsJPG;

		/// <summary>
		/// Gets the image as JPG.
		/// </summary>
		/// <returns>The image as JPG.</returns>
		public Task<byte[]> GetImageAsJpgAsync(int quality = 90, int desiredWidth = 0, int desiredHeight = 0)
		{
			if (InternalGetImageAsJPG == null)
				return null;

			return InternalGetImageAsJPG(new GetImageAsJpgArgs()
			{
				Quality = quality,
				DesiredWidth = desiredWidth,
				DesiredHeight = desiredHeight,
			});
		}

		internal Func<GetImageAsPngArgs, Task<byte[]>> InternalGetImageAsPNG;

		/// <summary>
		/// Gets the image as PNG
		/// </summary>
		/// <returns>The image as PNG.</returns>
		public Task<byte[]> GetImageAsPngAsync(int desiredWidth = 0, int desiredHeight = 0)
		{
			if (InternalGetImageAsPNG == null)
				return null;

			return InternalGetImageAsPNG(new GetImageAsPngArgs()
			{
				DesiredWidth = desiredWidth,
				DesiredHeight = desiredHeight,
			});
		}

		/// <summary>
		/// Invalidates cache for a specified image source.
		/// </summary>
		/// <param name="source">Image source.</param>
		/// <param name="cacheType">Cache type.</param>
		/// <param name = "removeSimilar">If set to <c>true</c> removes all image cache variants
		/// (downsampling and transformations variants)</param>
		public static async Task InvalidateCache(ImageSource source, Cache.CacheType cacheType, bool removeSimilar = false)
		{
			if (source is FileImageSource fileImageSource)
				await ImageService.Instance.InvalidateCacheEntryAsync(fileImageSource.File, cacheType, removeSimilar).ConfigureAwait(false);

			if (source is UriImageSource uriImageSource)
				await ImageService.Instance.InvalidateCacheEntryAsync(uriImageSource.Uri.OriginalString, cacheType, removeSimilar).ConfigureAwait(false);

			if (source is EmbeddedResourceImageSource embResourceSource)
				await ImageService.Instance.InvalidateCacheEntryAsync(embResourceSource.Uri.OriginalString, cacheType, removeSimilar).ConfigureAwait(false);
		}

		/// <summary>
		/// Invalidates cache for a specified key.
		/// </summary>
		/// <param name="key">Image key.</param>
		/// <param name="cacheType">Cache type.</param>
		/// <param name = "removeSimilar">If set to <c>true</c> removes all image cache variants
		/// (downsampling and transformations variants)</param>
		public static Task InvalidateCache(string key, CacheType cacheType, bool removeSimilar = false)
		{
			return ImageService.Instance.InvalidateCacheEntryAsync(key, cacheType, removeSimilar);
		}

		/// <summary>
		/// Occurs after image loading success.
		/// </summary>
		public event EventHandler<CachedImageEvents.SuccessEventArgs> Success;

		/// <summary>
		/// The SuccessCommandProperty.
		/// </summary>
		public static readonly BindableProperty SuccessCommandProperty = BindableProperty.Create(nameof(SuccessCommand), typeof(ICommand), typeof(CachedImage));

		/// <summary>
		/// Gets or sets the SuccessCommand.
		/// Occurs after image loading success.
		/// Command parameter: CachedImageEvents.SuccessEventArgs
		/// </summary>
		/// <value>The success command.</value>
		public ICommand SuccessCommand
		{
			get => (ICommand)GetValue(SuccessCommandProperty);
			set => SetValue(SuccessCommandProperty, value);
		}

		internal void OnSuccess(CachedImageEvents.SuccessEventArgs e)
		{
			Success?.Invoke(this, e);

			var successCommand = SuccessCommand;
			if (successCommand != null && successCommand.CanExecute(e))
				successCommand.Execute(e);
		}

		/// <summary>
		/// Occurs after image loading error.
		/// </summary>
		public event EventHandler<CachedImageEvents.ErrorEventArgs> Error;

		/// <summary>
		/// The ErrorCommandProperty.
		/// </summary>
		public static readonly BindableProperty ErrorCommandProperty = BindableProperty.Create(nameof(ErrorCommand), typeof(ICommand), typeof(CachedImage));

		/// <summary>
		/// Gets or sets the ErrorCommand.
		/// Occurs after image loading error.
		/// Command parameter: CachedImageEvents.ErrorEventArgs
		/// </summary>
		/// <value>The error command.</value>
		public ICommand ErrorCommand
		{
			get => (ICommand)GetValue(ErrorCommandProperty);
			set => SetValue(ErrorCommandProperty, value);
		}

		internal void OnError(CachedImageEvents.ErrorEventArgs e)
		{
			Error?.Invoke(this, e);

			var errorCommand = ErrorCommand;
			if (errorCommand != null && errorCommand.CanExecute(e))
				errorCommand.Execute(e);
		}

		/// <summary>
		/// Occurs after every image loading.
		/// </summary>
		public event EventHandler<CachedImageEvents.FinishEventArgs> Finish;

		/// <summary>
		/// The FinishCommandProperty.
		/// </summary>
		public static readonly BindableProperty FinishCommandProperty = BindableProperty.Create(nameof(FinishCommand), typeof(ICommand), typeof(CachedImage));

		/// <summary>
		/// Gets or sets the FinishCommand.
		/// Occurs after every image loading.
		/// Command parameter: CachedImageEvents.FinishEventArgs
		/// </summary>
		/// <value>The finish command.</value>
		public ICommand FinishCommand
		{
			get => (ICommand)GetValue(FinishCommandProperty);
			set => SetValue(FinishCommandProperty, value);
		}

		internal void OnFinish(CachedImageEvents.FinishEventArgs e)
		{
			Finish?.Invoke(this, e);

			var finishCommand = FinishCommand;
			if (finishCommand != null && finishCommand.CanExecute(e))
				finishCommand.Execute(e);
		}


		/// <summary>
		/// Occurs when an image starts downloading from web.
		/// </summary>
		public event EventHandler<CachedImageEvents.DownloadStartedEventArgs> DownloadStarted;

		/// <summary>
		/// The DownloadStartedCommandProperty.
		/// </summary>
		public static readonly BindableProperty DownloadStartedCommandProperty = BindableProperty.Create(nameof(DownloadStartedCommand), typeof(ICommand), typeof(CachedImage));

		/// <summary>
		/// Gets or sets the DownloadStartedCommand.
		///  Occurs when an image starts downloading from web.
		/// Command parameter: DownloadStartedEventArgs
		/// </summary>
		/// <value>The download started command.</value>
		public ICommand DownloadStartedCommand
		{
			get => (ICommand)GetValue(DownloadStartedCommandProperty);
			set => SetValue(DownloadStartedCommandProperty, value);
		}

		internal void OnDownloadStarted(CachedImageEvents.DownloadStartedEventArgs e)
		{
			DownloadStarted?.Invoke(this, e);

			var downloadStartedCommand = DownloadStartedCommand;
			if (downloadStartedCommand != null && downloadStartedCommand.CanExecute(e))
				downloadStartedCommand.Execute(e);
		}

		/// <summary>
		/// This callback can be used for reading download progress
		/// </summary>
		public event EventHandler<CachedImageEvents.DownloadProgressEventArgs> DownloadProgress;

		/// <summary>
		/// The DownloadProgressCommandProperty.
		/// </summary>
		public static readonly BindableProperty DownloadProgressCommandProperty = BindableProperty.Create(nameof(DownloadProgressCommand), typeof(ICommand), typeof(CachedImage));

		/// <summary>
		/// Gets or sets the DownloadProgressCommand.
		///  This callback can be used for reading download progress
		/// Command parameter: DownloadProgressEventArgs
		/// </summary>
		/// <value>The download started command.</value>
		public ICommand DownloadProgressCommand
		{
			get => (ICommand)GetValue(DownloadProgressCommandProperty);
			set => SetValue(DownloadProgressCommandProperty, value);
		}

		internal void OnDownloadProgress(CachedImageEvents.DownloadProgressEventArgs e)
		{
			DownloadProgress?.Invoke(this, e);

			var downloadProgressCommand = DownloadProgressCommand;
			if (downloadProgressCommand != null && downloadProgressCommand.CanExecute(e))
				downloadProgressCommand.Execute(e);
		}

		/// <summary>
		/// Called after file is succesfully written to the disk.
		/// </summary>
		public event EventHandler<CachedImageEvents.FileWriteFinishedEventArgs> FileWriteFinished;

		/// <summary>
		/// The FileWriteFinishedCommandProperty.
		/// </summary>
		public static readonly BindableProperty FileWriteFinishedCommandProperty = BindableProperty.Create(nameof(FileWriteFinishedCommand), typeof(ICommand), typeof(CachedImage));

		/// <summary>
		/// Gets or sets the FileWriteFinishedCommand.
		///  Called after file is succesfully written to the disk.
		/// Command parameter: FileWriteFinishedEventArgs
		/// </summary>
		/// <value>The download started command.</value>
		public ICommand FileWriteFinishedCommand
		{
			get => (ICommand)GetValue(FileWriteFinishedCommandProperty);
			set => SetValue(FileWriteFinishedCommandProperty, value);
		}

		internal void OnFileWriteFinished(CachedImageEvents.FileWriteFinishedEventArgs e)
		{
			FileWriteFinished?.Invoke(this, e);

			var fileWriteFinishedCommand = FileWriteFinishedCommand;
			if (fileWriteFinishedCommand != null && fileWriteFinishedCommand.CanExecute(e))
				fileWriteFinishedCommand.Execute(e);
		}

		/// <summary>
		/// The cache type property.
		/// </summary>
		public static readonly BindableProperty CacheTypeProperty = BindableProperty.Create(nameof(CacheType), typeof(CacheType?), typeof(CachedImage), default(CacheType?));

		/// <summary>
		/// Set the cache storage type, (Memory, Disk, All). by default cache is set to All.
		/// </summary>
		public CacheType? CacheType
		{
			get => (CacheType?)GetValue(CacheTypeProperty);
			set => SetValue(CacheTypeProperty, value);
		}

		/// <summary>
		/// Setups the on before image loading.
		/// You can add additional logic here to configure image loader settings before loading
		/// </summary>
		/// <param name="imageLoader">Image loader.</param>
		protected internal virtual void SetupOnBeforeImageLoading(Work.TaskParameter imageLoader)
		{
		}

		/// <summary>
		/// Setups the on before image loading.
		/// You can add additional logic here to configure image loader settings before loading
		/// </summary>
		/// <param name="imageLoader">Image loader.</param>
		/// <param name="source">Source.</param>
		/// <param name="loadingPlaceholderSource">Loading placeholder source.</param>
		/// <param name="errorPlaceholderSource">Error placeholder source.</param>
		protected internal virtual void SetupOnBeforeImageLoading(out Work.TaskParameter imageLoader, IImageSourceBinding source, IImageSourceBinding loadingPlaceholderSource, IImageSourceBinding errorPlaceholderSource)
		{
			if (source.ImageSource == Work.ImageSource.Url)
			{
				imageLoader = ImageService.Instance.LoadUrl(source.Path, CacheDuration);
			}
			else if (source.ImageSource == Work.ImageSource.CompiledResource)
			{
				imageLoader = ImageService.Instance.LoadCompiledResource(source.Path);
			}
			else if (source.ImageSource == Work.ImageSource.ApplicationBundle)
			{
				imageLoader = ImageService.Instance.LoadFileFromApplicationBundle(source.Path);
			}
			else if (source.ImageSource == Work.ImageSource.Filepath)
			{
				imageLoader = ImageService.Instance.LoadFile(source.Path);
			}
			else if (source.ImageSource == Work.ImageSource.Stream)
			{
				imageLoader = ImageService.Instance.LoadStream(source.Stream);
			}
			else if (source.ImageSource == Work.ImageSource.EmbeddedResource)
			{
				imageLoader = ImageService.Instance.LoadEmbeddedResource(source.Path);
			}
			else
			{
				imageLoader = null;
				return;
			}

			var widthRequest = (int)(double.IsPositiveInfinity(WidthRequest) ? 0 : Math.Max(0, WidthRequest));
			var heightRequest = (int)(double.IsPositiveInfinity(HeightRequest) ? 0 : Math.Max(0, HeightRequest));
			var width = (int)(double.IsPositiveInfinity(Width) ? 0 : Math.Max(0, Width));
			var height = (int)(double.IsPositiveInfinity(Height) ? 0 : Math.Max(0, Height));

			// CustomKeyFactory
			if (CacheKeyFactory != null)
			{
				var bindingContext = BindingContext;
				imageLoader.CacheKey(CacheKeyFactory.GetKey(Source, bindingContext));
			}

			// LoadingPlaceholder
			if (LoadingPlaceholder != null)
			{
				if (loadingPlaceholderSource != null)
					imageLoader.LoadingPlaceholder(loadingPlaceholderSource.Path, loadingPlaceholderSource.ImageSource);
			}

			// ErrorPlaceholder
			if (ErrorPlaceholder != null)
			{

				if (errorPlaceholderSource != null)
					imageLoader.ErrorPlaceholder(errorPlaceholderSource.Path, errorPlaceholderSource.ImageSource);
			}

			// Enable vector image source
			var vect1 = Source as IVectorImageSource;
			var vect2 = LoadingPlaceholder as IVectorImageSource;
			var vect3 = ErrorPlaceholder as IVectorImageSource;

			if (vect1 != null || vect2 != null || vect3 != null)
			{
				if (widthRequest == 0 && heightRequest == 0 && width == 0 && height == 0)
				{
					_reloadBecauseOfMissingSize = true;
					imageLoader = null;
					return;
				}

				var isWidthHeightRequestSet = widthRequest > 0 || heightRequest > 0; 

				if (vect1 != null)
				{
					var newVect = vect1.Clone();

					if (newVect.VectorWidth == 0 && newVect.VectorHeight == 0)
					{
						newVect.VectorWidth = isWidthHeightRequestSet ? widthRequest : width;
						newVect.VectorHeight = isWidthHeightRequestSet ? heightRequest : height;
						newVect.UseDipUnits = true;
					}

					imageLoader.WithCustomDataResolver(newVect.GetVectorDataResolver());
				}
				if (vect2 != null)
				{
					var newVect = vect2.Clone();

					if (newVect.VectorWidth == 0 && newVect.VectorHeight == 0)
					{
						newVect.VectorWidth = isWidthHeightRequestSet ? widthRequest : width;
						newVect.VectorHeight = isWidthHeightRequestSet ? heightRequest : height;
						newVect.UseDipUnits = true;
					}

					imageLoader.WithCustomLoadingPlaceholderDataResolver(newVect.GetVectorDataResolver());
				}
				if (vect3 != null)
				{
					var newVect = vect3.Clone();

					if (newVect.VectorWidth == 0 && newVect.VectorHeight == 0)
					{
						newVect.VectorWidth = isWidthHeightRequestSet ? widthRequest : width;
						newVect.VectorHeight = isWidthHeightRequestSet ? heightRequest : height;
						newVect.UseDipUnits = true;
					}

					imageLoader.WithCustomErrorPlaceholderDataResolver(newVect.GetVectorDataResolver());
				}
			}
			if (CustomDataResolver != null)
			{
				imageLoader.WithCustomDataResolver(CustomDataResolver);
				imageLoader.WithCustomLoadingPlaceholderDataResolver(CustomDataResolver);
				imageLoader.WithCustomErrorPlaceholderDataResolver(CustomDataResolver);
			}

			// Downsample
			if (DownsampleToViewSize && (widthRequest > 0 || heightRequest > 0))
			{
				imageLoader.DownSampleInDip(widthRequest, heightRequest);
			}
			else if (DownsampleToViewSize && (width > 0 || height > 0))
			{
				imageLoader.DownSampleInDip(width, height);
			}
			else if ((int)DownsampleHeight != 0 || (int)DownsampleWidth != 0)
			{
				if (DownsampleUseDipUnits)
					imageLoader.DownSampleInDip((int)DownsampleWidth, (int)DownsampleHeight);
				else
					imageLoader.DownSample((int)DownsampleWidth, (int)DownsampleHeight);
			}
			else if (DownsampleToViewSize)
			{
				_reloadBecauseOfMissingSize = true;
				imageLoader = null;

				return;
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

			if (LoadingDelay.HasValue)
			{
				imageLoader.Delay(LoadingDelay.Value);
			}

			imageLoader.DownloadStarted((downloadInformation) => OnDownloadStarted(new CachedImageEvents.DownloadStartedEventArgs(downloadInformation)));
			imageLoader.DownloadProgress((progress) => OnDownloadProgress(new CachedImageEvents.DownloadProgressEventArgs(progress)));
			imageLoader.FileWriteFinished((fileWriteInfo) => OnFileWriteFinished(new CachedImageEvents.FileWriteFinishedEventArgs(fileWriteInfo)));
			imageLoader.Error((exception) => OnError(new CachedImageEvents.ErrorEventArgs(exception)));
			imageLoader.Finish((work) => OnFinish(new CachedImageEvents.FinishEventArgs(work)));
			imageLoader.Success((imageInformation, loadingResult) => OnSuccess(new CachedImageEvents.SuccessEventArgs(imageInformation, loadingResult)));

			SetupOnBeforeImageLoading(imageLoader);
		}

		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);

			if(_reloadBecauseOfMissingSize)
			{
				_reloadBecauseOfMissingSize = false;

				if (width <= 0 && height <= 0)
				{
					ImageService.Instance.Config.Logger?.Error("Couldn't read view size for auto sizing");
				}

				ReloadImage();
			}
		}
	}
}

