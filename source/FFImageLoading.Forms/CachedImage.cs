using System;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using FFImageLoading.Forms.Args;
using System.Windows.Input;
using System.Threading;

namespace FFImageLoading.Forms
{
	/// <summary>
	/// CachedImage - Xamarin.Forms Image replacement with caching and downsampling capabilities
	/// </summary>
	public class CachedImage : View
	{
		public CachedImage()
		{
			Transformations = new List<Work.ITransformation>();
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
			get
			{
				return (Aspect)GetValue(AspectProperty);
			}
			set
			{
				SetValue(AspectProperty, value);
			}
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
		public bool IsLoading
		{
			get
			{
				return (bool)GetValue(IsLoadingProperty);
			}
		}

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
			get
			{
				return (bool)GetValue(IsOpaqueProperty);
			}
			set
			{
				SetValue(IsOpaqueProperty, value);
			}
		}

        /// <summary>
        /// The source property.
        /// </summary> 
        public static readonly BindableProperty SourceProperty = BindableProperty.Create(nameof (Source), typeof(ImageSource), typeof(CachedImage), default(ImageSource), BindingMode.OneWay, propertyChanged: OnSourcePropertyChanged);

        static void OnSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null) 
            {
                BindableObject.SetInheritedBindingContext(newValue as BindableObject, bindable.BindingContext);
            }
            ((CachedImage)bindable).InvalidateMeasure();
        }
			
		/// <summary>
		/// Gets or sets the source.
		/// </summary>
		/// <value>The source.</value>
		[TypeConverter(typeof(ImageSourceConverter))]
		public ImageSource Source
		{
			get
			{
				return (ImageSource)GetValue(SourceProperty);
			}
			set
			{
				SetValue(SourceProperty, value);
			}
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
        public static readonly BindableProperty RetryDelayProperty = BindableProperty.Create(nameof(RetryDelay), typeof(int), typeof(CachedImage), 250);

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
        public static readonly BindableProperty DownsampleWidthProperty = BindableProperty.Create(nameof(DownsampleWidth), typeof(double), typeof(CachedImage), 0d);

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
        public static readonly BindableProperty DownsampleHeightProperty = BindableProperty.Create(nameof(DownsampleHeight), typeof(double), typeof(CachedImage), 0d);

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
		/// The downsample to view size property.
		/// </summary>
        public static readonly BindableProperty DownsampleToViewSizeProperty = BindableProperty.Create(nameof(DownsampleToViewSize), typeof(bool), typeof(CachedImage), false);

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
        public static readonly BindableProperty CacheDurationProperty = BindableProperty.Create(nameof(CacheDuration), typeof(TimeSpan), typeof(CachedImage), TimeSpan.FromDays(90));

		/// <summary>
		/// How long the file will be cached on disk.
		/// </summary>
		public TimeSpan CacheDuration
		{
			get
			{
				return (TimeSpan)GetValue(CacheDurationProperty); 
			}
			set
			{
				SetValue(CacheDurationProperty, value); 
			}
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
            get
            {
                return (Work.LoadingPriority)GetValue(LoadingPriorityProperty); 
            }
            set
            {
                SetValue(LoadingPriorityProperty, value); 
            }
        }

		/// <summary>
		/// The transparency enabled property.
		/// </summary>
        public static readonly BindableProperty TransparencyEnabledProperty = BindableProperty.Create(nameof(TransparencyEnabled), typeof(bool?), typeof(CachedImage), default(bool?));

		/// <summary>
		/// Indicates if the transparency channel should be loaded. By default this value comes from ImageService.Instance.Config.LoadWithTransparencyChannel.
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
        public static readonly BindableProperty FadeAnimationEnabledProperty = BindableProperty.Create(nameof(FadeAnimationEnabled), typeof(bool?), typeof(CachedImage), default(bool?));

		/// <summary>
		/// Indicates if the fade animation effect should be enabled. By default this value comes from ImageService.Instance.Config.FadeAnimationEnabled.
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
        public static readonly BindableProperty LoadingPlaceholderProperty = BindableProperty.Create(nameof(LoadingPlaceholder), typeof(ImageSource), typeof(CachedImage), default(ImageSource));

		/// <summary>
		/// Gets or sets the loading placeholder image.
		/// </summary>
		[TypeConverter(typeof(ImageSourceConverter))]
		public ImageSource LoadingPlaceholder
		{
			get
			{
				return (ImageSource)GetValue(LoadingPlaceholderProperty); 
			}
			set
			{
				SetValue(LoadingPlaceholderProperty, value); 
			}
		}

		/// <summary>
		/// The error placeholder property.
		/// </summary>
        public static readonly BindableProperty ErrorPlaceholderProperty = BindableProperty.Create(nameof(ErrorPlaceholder), typeof(ImageSource), typeof(CachedImage), default(ImageSource));

		/// <summary>
		/// Gets or sets the error placeholder image.
		/// </summary>
		[TypeConverter(typeof(ImageSourceConverter))]
		public ImageSource ErrorPlaceholder
		{
			get
			{
				return (ImageSource)GetValue(ErrorPlaceholderProperty); 
			}
			set
			{
				SetValue(ErrorPlaceholderProperty, value); 
			}
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
			get
			{
				return (bool?)GetValue(TransformPlaceholdersProperty);
			}
			set
			{
				SetValue(TransformPlaceholdersProperty, value);
			}
		}

		/// <summary>
		/// The transformations property.
		/// </summary>
        public static readonly BindableProperty TransformationsProperty = BindableProperty.Create(nameof(Transformations), typeof(List<Work.ITransformation>), typeof(CachedImage), new List<Work.ITransformation>());

		/// <summary>
		/// Gets or sets the transformations.
		/// </summary>
		/// <value>The transformations.</value>
		public List<Work.ITransformation> Transformations
		{
			get
			{
                return (List<Work.ITransformation>)GetValue(TransformationsProperty); 
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

		//
		// Methods
		//
		protected override void OnBindingContextChanged()
		{
			if (this.Source != null)
			{
				BindableObject.SetInheritedBindingContext(Source, BindingContext);
			}

			base.OnBindingContextChanged();
		}

		internal void InvalidateViewMeasure()
		{
			// this.OnPropertyChanged(Image.SourceProperty.PropertyName);
			InvalidateMeasure();
		}

		protected override SizeRequest OnSizeRequest(double widthConstraint, double heightConstraint)
		{
			SizeRequest sizeRequest = base.OnSizeRequest(double.PositiveInfinity, double.PositiveInfinity);
			double num = sizeRequest.Request.Width / sizeRequest.Request.Height;
			double num2 = widthConstraint / heightConstraint;
			double width = sizeRequest.Request.Width;
			double height = sizeRequest.Request.Height;
			if (width == 0 || height == 0)
			{
				return new SizeRequest(new Size(0, 0));
			}
			double num3 = width;
			double num4 = height;
			if (num2 > num)
			{
				switch (this.Aspect)
				{
					case Aspect.AspectFit:
					case Aspect.AspectFill:
						num4 = Math.Min(height, heightConstraint);
						num3 = width * (num4 / height);
						break;
					case Aspect.Fill:
						num3 = Math.Min(width, widthConstraint);
						num4 = height * (num3 / width);
						break;
				}
			}
			else if (num2 < num)
			{
				switch (this.Aspect)
				{
					case Aspect.AspectFit:
					case Aspect.AspectFill:
						num3 = Math.Min(width, widthConstraint);
						num4 = height * (num3 / width);
						break;
					case Aspect.Fill:
						num4 = Math.Min(height, heightConstraint);
						num3 = width * (num4 / height);
						break;
				}
			}
			else
			{
				num3 = Math.Min(width, widthConstraint);
				num4 = height * (num3 / width);
			} 
			return new SizeRequest(new Size(num3, num4));
		}

		internal Action InternalReloadImage;
			
		/// <summary>
		/// Reloads the image.
		/// </summary>
		public void ReloadImage()
		{
			if (InternalReloadImage != null && Source != null)
			{
				InternalReloadImage();
			}
		}

		internal Action InternalCancel;

        /// <summary>
        /// Cancels image loading tasks
        /// </summary>
		public void Cancel()
		{
			if (InternalCancel != null) 
			{
				InternalCancel();
			}
		}

		internal Func<GetImageAsJpgArgs, Task<byte[]>> InternalGetImageAsJPG; 

		/// <summary>
		/// Gets the image as JPG.
		/// </summary>
		/// <returns>The image as JPG.</returns>
		public Task<byte[]> GetImageAsJpgAsync(int quality = 90, int desiredWidth = 0, int desiredHeight = 0)
		{
			if (InternalGetImageAsJPG == null)
				return null;

			return InternalGetImageAsJPG(new GetImageAsJpgArgs() {
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

			return InternalGetImageAsPNG(new GetImageAsPngArgs() {
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
			var fileImageSource = source as FileImageSource;

			if (fileImageSource != null)
				await ImageService.Instance.InvalidateCacheEntryAsync(fileImageSource.File, cacheType, removeSimilar);

			var uriImageSource = source as UriImageSource;

			if (uriImageSource != null)
				await ImageService.Instance.InvalidateCacheEntryAsync(uriImageSource.Uri.ToString(), cacheType, removeSimilar);
		}

		/// <summary>
		/// Invalidates cache for a specified key.
		/// </summary>
		/// <param name="source">Image key.</param>
		/// <param name="cacheType">Cache type.</param>
		/// <param name = "removeSimilar">If set to <c>true</c> removes all image cache variants 
		/// (downsampling and transformations variants)</param>
		public static Task InvalidateCache(string key, Cache.CacheType cacheType, bool removeSimilar = false)
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
		public static readonly BindableProperty SuccessCommandProperty = BindableProperty.Create<CachedImage, ICommand> (w => w.SuccessCommand, null);

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

		internal void OnSuccess(CachedImageEvents.SuccessEventArgs e) 
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
		public event EventHandler<CachedImageEvents.ErrorEventArgs> Error;

		/// <summary>
		/// The ErrorCommandProperty.
		/// </summary>
		public static readonly BindableProperty ErrorCommandProperty = BindableProperty.Create<CachedImage, ICommand> (w => w.ErrorCommand, null);

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

		internal void OnError(CachedImageEvents.ErrorEventArgs e) 
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
		public event EventHandler<CachedImageEvents.FinishEventArgs> Finish;

		/// <summary>
		/// The FinishCommandProperty.
		/// </summary>
		public static readonly BindableProperty FinishCommandProperty = BindableProperty.Create<CachedImage, ICommand> (w => w.FinishCommand, null);

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

		internal void OnFinish(CachedImageEvents.FinishEventArgs e) 
		{
			var handler = Finish;
			if (handler != null) handler(this, e);

			var finishCommand = FinishCommand;
			if (finishCommand != null && finishCommand.CanExecute(e))
				finishCommand.Execute(e);
		}
    }
}

