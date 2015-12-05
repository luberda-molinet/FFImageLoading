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
using FFImageLoading.Views;
using Android.Graphics.Drawables;
using Android.Graphics;
using System.IO;
using System.Threading.Tasks;

[assembly: ExportRenderer(typeof(CachedImage), typeof(CachedImageRenderer))]
namespace FFImageLoading.Forms.Droid
{
	/// <summary>
	/// CachedImage Implementation
	/// </summary>
	[Preserve(AllMembers=true)]
	public class CachedImageRenderer : ViewRenderer<CachedImage, ImageViewAsync>
	{
		/// <summary>
		///   Used for registration with dependency service
		/// </summary>
		public static void Init()
		{
			CachedImage.InternalClearCache = new Action<FFImageLoading.Cache.CacheType>(ClearCache);
			CachedImage.InternalInvalidateCache = new Action<string, FFImageLoading.Cache.CacheType>(InvalidateCache);
        }

		private static void InvalidateCache(string key, Cache.CacheType cacheType)
        {
            ImageService.Invalidate(key, cacheType);
        }

		private static void ClearCache(Cache.CacheType cacheType)
        {
			switch (cacheType)
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

        private bool _isDisposed;
		private IScheduledWork _currentTask;

		public CachedImageRenderer()
		{
			AutoPackage = false;
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
			} else {
				e.OldElement.Cancelled -= Cancel;
			}
			if (e.NewElement != null)
			{
				e.NewElement.Cancelled += Cancel;
				e.NewElement.InternalGetImageAsJPG = new Func<int, int, int, Task<byte[]>>(GetImageAsJPG);
				e.NewElement.InternalGetImageAsPNG = new Func<int, int, int, Task<byte[]>>(GetImageAsPNG);
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
				CachedImageView formsImageView = Control as CachedImageView;

				if (formsImageView == null)
					return;
					
				((IElementController)Element).SetValueFromRenderer(CachedImage.IsLoadingPropertyKey, true);

				if (Element != null && object.Equals(Element.Source, source) && !_isDisposed)
				{
					Cancel(this, EventArgs.Empty);
					TaskParameter imageLoader = null;

					var ffSource = ImageSourceBinding.GetImageSourceBinding(source);

					if (ffSource == null)
					{
						if (Control != null)
							Control.SetImageDrawable(null);	
						
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
						if ((int)Element.DownsampleHeight != 0 || (int)Element.DownsampleWidth != 0)
						{
                            if (Element.DownsampleHeight > Element.DownsampleWidth)
                            {
                                imageLoader.DownSample(height: (int)Element.DownsampleHeight);
                            }
                            else
                            {
                                imageLoader.DownSample(width: (int)Element.DownsampleWidth);
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

						var element = Element;

						imageLoader.Finish((work) => {
							element.OnFinish(new CachedImageEvents.FinishEventArgs(work));
							ImageLoadingFinished(element);
						});

						imageLoader.Success((imageSize, loadingResult) => 
							element.OnSuccess(new CachedImageEvents.SuccessEventArgs(imageSize, loadingResult)));

						imageLoader.Error((exception) => 
							element.OnError(new CachedImageEvents.ErrorEventArgs(exception)));

						_currentTask = imageLoader.Into(Control);	
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

		private void Cancel(object sender, EventArgs args)
		{
			if (_currentTask != null && !_currentTask.IsCancelled) {
				_currentTask.Cancel ();
			}
		}

		private Task<byte[]> GetImageAsJPG(int quality, int desiredWidth = 0, int desiredHeight = 0)
		{
			return GetImageAsByte(Bitmap.CompressFormat.Jpeg, quality, desiredWidth, desiredHeight);
		}

		private Task<byte[]> GetImageAsPNG(int quality, int desiredWidth = 0, int desiredHeight = 0)
		{
			return GetImageAsByte(Bitmap.CompressFormat.Png, quality, desiredWidth, desiredHeight);
		}

		private async Task<byte[]> GetImageAsByte(Bitmap.CompressFormat format, int quality, int desiredWidth, int desiredHeight)
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
				await bitmap.CompressAsync(format, quality, stream);
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

