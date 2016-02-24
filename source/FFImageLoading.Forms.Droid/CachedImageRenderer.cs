using Android.Widget;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using FFImageLoading;
using FFImageLoading.Work;
using FFImageLoading.Forms.Droid;
using FFImageLoading.Forms;
using Android.Runtime;
using Android.Graphics.Drawables;
using Android.Graphics;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Extensions;
using FFImageLoading.Forms.Args;
using System.Threading;

[assembly: ExportRenderer(typeof(CachedImage), typeof(CachedImageRenderer))]
namespace FFImageLoading.Forms.Droid
{
	/// <summary>
	/// CachedImage Implementation
	/// </summary>
	[Preserve(AllMembers=true)]
	public class CachedImageRenderer : ViewRenderer<CachedImage, CachedImageView>
	{
		/// <summary>
		///   Used for registration with dependency service
		/// </summary>
		public static void Init()
		{
            CachedImage.InternalClearCache = new Func<FFImageLoading.Cache.CacheType, Task>(ClearCacheAsync);
            CachedImage.InternalInvalidateCache = new Func<string, FFImageLoading.Cache.CacheType, bool, Task>(InvalidateCacheEntryAsync);
			CachedImage.InternalSetPauseWork = new Action<bool>(SetPauseWork);
            CachedImage.InternalDownloadImageAndAddToDiskCache = new Func<string, CancellationToken, TimeSpan?, string, Task<bool>>(DownloadImageAndAddToDiskCache);
        }

        private static Task InvalidateCacheEntryAsync(string key, Cache.CacheType cacheType, bool removeSimilar)
        {
            return ImageService.InvalidateCacheEntryAsync(key, cacheType, removeSimilar);
        }

		private static async Task ClearCacheAsync(Cache.CacheType cacheType)
        {
			switch (cacheType)
            {
                case Cache.CacheType.Memory:
                    ImageService.InvalidateMemoryCache();
                    break;
                case Cache.CacheType.Disk:
                    await ImageService.InvalidateDiskCacheAsync().ConfigureAwait(false);
                    break;
                case Cache.CacheType.All:
                    ImageService.InvalidateMemoryCache();
                    await ImageService.InvalidateDiskCacheAsync().ConfigureAwait(false);
                    break;
            }
        }

		private static void SetPauseWork(bool pauseWork)
		{
			ImageService.SetPauseWork(pauseWork);
		}

        private static Task<bool> DownloadImageAndAddToDiskCache(string imageUrl, CancellationToken cancellationToken, TimeSpan? duration = null, string customCacheKey = null)
        {
            return ImageService.DownloadImageAndAddToDiskCacheAsync(imageUrl, cancellationToken, duration, customCacheKey);
        }

        private bool _isDisposed;
		private IScheduledWork _currentTask;

		public CachedImageRenderer()
		{
			AutoPackage = false;
		}

		public CachedImageRenderer(IntPtr javaReference, JniHandleOwnership transfer) : this()
		{
		}

		protected override void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				_isDisposed = true;
				base.Dispose(disposing);
			}
		}

		protected override void OnElementChanged(ElementChangedEventArgs<CachedImage> e)
		{
			base.OnElementChanged(e);

			if (e.OldElement == null)
			{
				CachedImageView nativeControl = new CachedImageView(Context);
				SetNativeControl(nativeControl);
			} 

			if (e.NewElement != null)
			{
				e.NewElement.InternalReloadImage = new Action(ReloadImage);
				e.NewElement.InternalCancel = new Action(Cancel);
				e.NewElement.InternalGetImageAsJPG = new Func<GetImageAsJpgArgs, Task<byte[]>>(GetImageAsJpgAsync);
				e.NewElement.InternalGetImageAsPNG = new Func<GetImageAsPngArgs, Task<byte[]>>(GetImageAsPngAsync);
			}

			UpdateBitmap(e.OldElement);
			UpdateAspect();
		}
			
		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == CachedImage.SourceProperty.PropertyName)
			{
				UpdateBitmap(null);	
			}
			if (e.PropertyName == CachedImage.AspectProperty.PropertyName)
			{
				UpdateAspect();
			}
		}

		private void UpdateAspect()
		{
			if (Element.Aspect == Aspect.AspectFill)
				Control.SetScaleType(ImageView.ScaleType.CenterCrop);

			else if (Element.Aspect == Aspect.Fill)
				Control.SetScaleType(ImageView.ScaleType.FitXy);

			else 
				Control.SetScaleType(ImageView.ScaleType.FitCenter);
		}
			
		private void UpdateBitmap(CachedImage previous = null)
		{
			if (previous == null || !object.Equals(previous.Source, Element.Source))
			{
				Xamarin.Forms.ImageSource source = Element.Source;
				var imageView = Control;

				if (imageView == null)
					return;

				((IElementController)Element).SetValueFromRenderer(CachedImage.IsLoadingPropertyKey, true);

				if (Element != null && object.Equals(Element.Source, source) && !_isDisposed)
				{
					Cancel();
					TaskParameter imageLoader = null;

					var ffSource = ImageSourceBinding.GetImageSourceBinding(source);

					if (ffSource == null)
					{
						if (imageView != null)
							imageView.SetImageDrawable(null);	

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
						// CustomKeyFactory
						if (Element.CacheKeyFactory != null)
						{
							var bindingContext = Element.BindingContext;
							imageLoader.CacheKey(Element.CacheKeyFactory.GetKey(source, bindingContext));
						}

						// LoadingPlaceholder
						if (Element.LoadingPlaceholder != null)
						{
							var placeholderSource = ImageSourceBinding.GetImageSourceBinding(Element.LoadingPlaceholder);
							if (placeholderSource != null)
								imageLoader.LoadingPlaceholder(placeholderSource.Path, placeholderSource.ImageSource);
						}

						// ErrorPlaceholder
						if (Element.ErrorPlaceholder != null)
						{
							var placeholderSource = ImageSourceBinding.GetImageSourceBinding(Element.ErrorPlaceholder);
							if (placeholderSource != null)
								imageLoader.ErrorPlaceholder(placeholderSource.Path, placeholderSource.ImageSource);
						}

						// Downsample
						if (Element.DownsampleToViewSize && (Element.Width > 0 || Element.Height > 0))
						{
							if (Element.Height > Element.Width)
							{
								imageLoader.DownSample(height: Element.Height.DpToPixels());
							}
							else
							{
								imageLoader.DownSample(width: Element.Width.DpToPixels());
							}
						}
						else if (Element.DownsampleToViewSize && (Element.WidthRequest > 0 || Element.HeightRequest > 0))
						{
							if (Element.HeightRequest > Element.WidthRequest)
							{
								imageLoader.DownSample(height: Element.HeightRequest.DpToPixels());
							}
							else
							{
								imageLoader.DownSample(width: Element.WidthRequest.DpToPixels());
							}
						}
						else if ((int)Element.DownsampleHeight != 0 || (int)Element.DownsampleWidth != 0)
						{
							if (Element.DownsampleHeight > Element.DownsampleWidth)
							{
								imageLoader.DownSample(height: Element.DownsampleUseDipUnits 
									? Element.DownsampleHeight.DpToPixels() : (int)Element.DownsampleHeight);
							}
							else
							{
								imageLoader.DownSample(width: Element.DownsampleUseDipUnits 
									? Element.DownsampleWidth.DpToPixels() : (int)Element.DownsampleWidth);
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

						// TransformPlaceholders
						if (Element.TransformPlaceholders.HasValue)
							imageLoader.TransformPlaceholders(Element.TransformPlaceholders.Value);

						// Transformations
						if (Element.Transformations != null && Element.Transformations.Count > 0)
						{
							imageLoader.Transform(Element.Transformations);
						}

						var element = Element;

						imageLoader.Finish((work) => {
							element.OnFinish(new CachedImageEvents.FinishEventArgs(work));
							ImageLoadingFinished(element);
						});
                            
						imageLoader.Success((imageInformation, loadingResult) => 
                            element.OnSuccess(new CachedImageEvents.SuccessEventArgs(imageInformation, loadingResult)));

						imageLoader.Error((exception) => 
							element.OnError(new CachedImageEvents.ErrorEventArgs(exception)));

						_currentTask = imageLoader.Into(imageView);	
					}
				}
			}
		}

		private void ImageLoadingFinished(CachedImage element)
		{
			if (element != null && !_isDisposed)
			{
				((IElementController)element).SetValueFromRenderer(CachedImage.IsLoadingPropertyKey, false);
				((IVisualElementController)element).NativeSizeChanged();	
				element.InvalidateViewMeasure();
			}
		}

		private void ReloadImage()
		{
			UpdateBitmap(null);
		}

		private void Cancel()
		{
			if (_currentTask != null && !_currentTask.IsCancelled) 
			{
				_currentTask.Cancel();
			}
		}

		private Task<byte[]> GetImageAsJpgAsync(GetImageAsJpgArgs args)
		{
			return GetImageAsByteAsync(Bitmap.CompressFormat.Jpeg, args.Quality, args.DesiredWidth, args.DesiredHeight);
		}

		private Task<byte[]> GetImageAsPngAsync(GetImageAsPngArgs args)
		{
			return GetImageAsByteAsync(Bitmap.CompressFormat.Png, 90, args.DesiredWidth, args.DesiredHeight);
		}

		private async Task<byte[]> GetImageAsByteAsync(Bitmap.CompressFormat format, int quality, int desiredWidth, int desiredHeight)
		{
			if (Control == null)
				return null;

			var drawable = Control.Drawable as BitmapDrawable;

			if (drawable == null || drawable.Bitmap == null)
				return null;

			Bitmap bitmap = drawable.Bitmap;

			if (desiredWidth != 0 || desiredHeight != 0)
			{
				double widthRatio = (double)desiredWidth / (double)bitmap.Width;
				double heightRatio = (double)desiredHeight / (double)bitmap.Height;

				double scaleRatio = Math.Min(widthRatio, heightRatio);

				if (desiredWidth == 0)
					scaleRatio = heightRatio;

				if (desiredHeight == 0)
					scaleRatio = widthRatio;

				int aspectWidth = (int)((double)bitmap.Width * scaleRatio);
				int aspectHeight = (int)((double)bitmap.Height * scaleRatio);

				bitmap = Bitmap.CreateScaledBitmap(bitmap, aspectWidth, aspectHeight, true);
			}

			using (var stream = new MemoryStream())
			{
				await bitmap.CompressAsync(format, quality, stream).ConfigureAwait(false);
				var compressed = stream.ToArray();

				if (desiredWidth != 0 || desiredHeight != 0)
				{
					bitmap.Recycle();
					bitmap.Dispose();
				}

				return compressed;
			}
		}
	}
}

