using Android.Widget;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using FFImageLoading.Work;
using FFImageLoading.Forms.Droid;
using FFImageLoading.Forms;
using Android.Runtime;
using Android.Graphics.Drawables;
using Android.Graphics;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Forms.Args;
using FFImageLoading.Helpers;
using FFImageLoading.Views;

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
        }

        private bool _isDisposed;
		private IScheduledWork _currentTask;
		private ImageSourceBinding _lastImageSource;

		public CachedImageRenderer()
		{
			AutoPackage = false;
		}

		public CachedImageRenderer(IntPtr javaReference, JniHandleOwnership transfer) : this()
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
				var nativeControl = new CachedImageView(Context);
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
			base.OnElementPropertyChanged(sender, e);

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
			Xamarin.Forms.ImageSource source = Element.Source;

			var imageView = Control;

			var ffSource = ImageSourceBinding.GetImageSourceBinding(source, Element);
			var placeholderSource = ImageSourceBinding.GetImageSourceBinding(Element.LoadingPlaceholder, Element);

			if (previous != null && _lastImageSource != null && ffSource != null && !ffSource.Equals(_lastImageSource)
				&& (string.IsNullOrWhiteSpace(placeholderSource?.Path) || placeholderSource?.Stream != null))
			{
				_lastImageSource = null;

				if (imageView != null)
					imageView.SkipInvalidate();

				Control.SetImageResource(global::Android.Resource.Color.Transparent);
			}

			Element.SetIsLoading(true);

			if (Element != null && object.Equals(Element.Source, source) && !_isDisposed)
			{
				Cancel();
				TaskParameter imageLoader = null;

				if (ffSource == null)
				{
					//if (imageView != null)
						imageView.SetImageResource(global::Android.Resource.Color.Transparent);

					ImageLoadingFinished(Element);
				}
				else if (ffSource.ImageSource == FFImageLoading.Work.ImageSource.Url)
				{
					imageLoader = ImageService.Instance.LoadUrl(ffSource.Path, Element.CacheDuration);
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
					// CustomKeyFactory
					if (Element.CacheKeyFactory != null)
					{
						var bindingContext = Element.BindingContext;
						imageLoader.CacheKey(Element.CacheKeyFactory.GetKey(source, bindingContext));
					}

					// LoadingPlaceholder
					if (Element.LoadingPlaceholder != null)
					{
						if (placeholderSource != null)
							imageLoader.LoadingPlaceholder(placeholderSource.Path, placeholderSource.ImageSource);
					}

					// ErrorPlaceholder
					if (Element.ErrorPlaceholder != null)
					{
						var errorPlaceholderSource = ImageSourceBinding.GetImageSourceBinding(Element.ErrorPlaceholder, Element);
						if (errorPlaceholderSource != null)
							imageLoader.ErrorPlaceholder(errorPlaceholderSource.Path, errorPlaceholderSource.ImageSource);
					}

					// Enable vector image source
					var vect1 = Element.Source as IVectorImageSource;
					var vect2 = Element.LoadingPlaceholder as IVectorImageSource;
					var vect3 = Element.ErrorPlaceholder as IVectorImageSource;
					if (vect1 != null)
					{
						imageLoader.WithCustomDataResolver(vect1.GetVectorDataResolver());
					}
					if (vect2 != null)
					{
						imageLoader.WithCustomLoadingPlaceholderDataResolver(vect2.GetVectorDataResolver());
					}
					if (vect3 != null)
					{
						imageLoader.WithCustomErrorPlaceholderDataResolver(vect3.GetVectorDataResolver());
					}
					if (Element.CustomDataResolver != null)
					{
						imageLoader.WithCustomDataResolver(Element.CustomDataResolver);
						imageLoader.WithCustomLoadingPlaceholderDataResolver(Element.CustomDataResolver);
						imageLoader.WithCustomErrorPlaceholderDataResolver(Element.CustomDataResolver);
					}

					// Downsample
					if (Element.DownsampleToViewSize && (Element.Width > 0 || Element.Height > 0))
					{
						if (Element.Height > Element.Width)
						{
							imageLoader.DownSampleInDip(height: (int)Element.Height);
						}
						else
						{
							imageLoader.DownSampleInDip(width: (int)Element.Width);
						}
					}
					else if (Element.DownsampleToViewSize && (Element.WidthRequest > 0 || Element.HeightRequest > 0))
					{
						if (Element.HeightRequest > Element.WidthRequest)
						{
							imageLoader.DownSampleInDip(height: (int)Element.HeightRequest);
						}
						else
						{
							imageLoader.DownSampleInDip(width: (int)Element.WidthRequest);
						}
					}
					else if ((int)Element.DownsampleHeight != 0 || (int)Element.DownsampleWidth != 0)
					{
						if (Element.DownsampleHeight > Element.DownsampleWidth)
						{
							if (Element.DownsampleUseDipUnits)
								imageLoader.DownSampleInDip(height: (int)Element.DownsampleHeight);
							else
								imageLoader.DownSample(height: (int)Element.DownsampleHeight);
						}
						else
						{
							if (Element.DownsampleUseDipUnits)
								imageLoader.DownSampleInDip(width: (int)Element.DownsampleWidth);
							else
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

					if (Element.BitmapOptimizations.HasValue)
						imageLoader.BitmapOptimizations(Element.BitmapOptimizations.Value);

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

					imageLoader.WithPriority(Element.LoadingPriority);
					if (Element.CacheType.HasValue)
					{
						imageLoader.WithCache(Element.CacheType.Value);
					}

					if (Element.LoadingDelay.HasValue)
					{
						imageLoader.Delay(Element.LoadingDelay.Value);
					}

					var element = Element;

					imageLoader.Finish((work) =>
					{
						element.OnFinish(new CachedImageEvents.FinishEventArgs(work));
						ImageLoadingFinished(element);
					});

					imageLoader.Success((imageInformation, loadingResult) =>
					{
						element.OnSuccess(new CachedImageEvents.SuccessEventArgs(imageInformation, loadingResult));
						_lastImageSource = ffSource;
					});

					imageLoader.Error((exception) =>
						element.OnError(new CachedImageEvents.ErrorEventArgs(exception)));

					imageLoader.DownloadStarted((downloadInformation) =>
						element.OnDownloadStarted(new CachedImageEvents.DownloadStartedEventArgs(downloadInformation)));

					imageLoader.DownloadProgress((progress) =>
						element.OnDownloadProgress(new CachedImageEvents.DownloadProgressEventArgs(progress)));

					imageLoader.FileWriteFinished((fileWriteInfo) =>
						element.OnFileWriteFinished(new CachedImageEvents.FileWriteFinishedEventArgs(fileWriteInfo)));

                    element.SetupOnBeforeImageLoading(imageLoader);

					_currentTask = imageLoader.Into(imageView);
				}
			}
		}

		private void ImageLoadingFinished(CachedImage element)
		{
			MainThreadDispatcher.Instance.Post(() =>
			{
				if (element != null && !_isDisposed)
				{
					Element.SetIsLoading(false);
					((IVisualElementController)element).NativeSizeChanged();
				}
			});
		}

		private void ReloadImage()
		{
			UpdateBitmap(null);
		}

		private void Cancel()
		{
            try
            {
                var taskToCancel = _currentTask;
                if (taskToCancel != null && !taskToCancel.IsCancelled)
                {
                    taskToCancel?.Cancel();
                }
            }
            catch (Exception ex)
            {
                ImageService.Instance.Config.Logger.Error(ex.Message, ex);
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

		protected override CachedImageView CreateNativeControl()
		{
			return new CachedImageView(Context);
		}
	}
}

