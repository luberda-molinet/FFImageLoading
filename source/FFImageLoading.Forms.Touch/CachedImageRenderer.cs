using CoreGraphics;
using System;
using System.ComponentModel;
using UIKit;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using FFImageLoading.Work;
using Foundation;
using FFImageLoading.Forms;
using FFImageLoading.Forms.Touch;
using FFImageLoading.Extensions;
using System.Threading.Tasks;
using FFImageLoading.Helpers;
using FFImageLoading.Forms.Args;

[assembly:ExportRenderer(typeof (CachedImage), typeof (CachedImageRenderer))]
namespace FFImageLoading.Forms.Touch
{
	/// <summary>
	/// CachedImage Implementation
	/// </summary>
	[Preserve(AllMembers = true)]
	public class CachedImageRenderer : ViewRenderer<CachedImage, UIImageView>
	{
		private bool _isDisposed;
		private IScheduledWork _currentTask;
		private ImageSourceBinding _lastImageSource;

		/// <summary>
		///   Used for registration with dependency service
		/// </summary>
		public static new void Init()
		{
			// needed because of this STUPID linker issue: https://bugzilla.xamarin.com/show_bug.cgi?id=31076
			#pragma warning disable 0219
			var dummy = new CachedImageRenderer();
			#pragma warning restore 0219
		}

        protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing && Control != null)
			{
				UIImage image = Control.Image;
				if (image != null)
				{
					image.Dispose();
					image = null;
				}
			}

			_isDisposed = true;
			base.Dispose(disposing);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<CachedImage> e)
		{
			if (Control == null)
			{
				SetNativeControl(new UIImageView(CGRect.Empty) {
					ContentMode = UIViewContentMode.ScaleAspectFit,
					ClipsToBounds = true
				});
			}

			if (e.NewElement != null)
			{
				SetAspect();
				SetImage(e.OldElement);
				SetOpacity();

				e.NewElement.InternalReloadImage = new Action(ReloadImage);
				e.NewElement.InternalCancel = new Action(Cancel);
				e.NewElement.InternalGetImageAsJPG = new Func<GetImageAsJpgArgs, Task<byte[]>>(GetImageAsJpgAsync);
				e.NewElement.InternalGetImageAsPNG = new Func<GetImageAsPngArgs, Task<byte[]>>(GetImageAsPngAsync);
			}
			base.OnElementChanged(e);
		}
			
		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == CachedImage.SourceProperty.PropertyName)
			{
				SetImage();
			}
			if (e.PropertyName == CachedImage.IsOpaqueProperty.PropertyName)
			{
				SetOpacity();
			}
			if (e.PropertyName == CachedImage.AspectProperty.PropertyName)
			{
				SetAspect();
			}
		}

		private void SetAspect()
		{
			Control.ContentMode = Element.Aspect.ToUIViewContentMode();
		}

		private void SetOpacity()
		{
			Control.Opaque = Element.IsOpaque;
		}

		private void SetImage(CachedImage oldElement = null)
		{
			Xamarin.Forms.ImageSource source = Element.Source;

			var ffSource = ImageSourceBinding.GetImageSourceBinding(source, Element);
			var placeholderSource = ImageSourceBinding.GetImageSourceBinding(Element.LoadingPlaceholder, Element);

			if (oldElement != null && _lastImageSource != null && ffSource != null && !ffSource.Equals(_lastImageSource)
				&& (string.IsNullOrWhiteSpace(placeholderSource?.Path) || placeholderSource?.Stream != null))
			{
				_lastImageSource = null;
				Control.Image = null;
			}

			Element.SetIsLoading(true);

			Cancel();
			TaskParameter imageLoader = null;

			if (ffSource == null)
			{
				if (Control != null)
					Control.Image = null;
				
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

				imageLoader.Finish((work) => {
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
				
				_currentTask = imageLoader.Into(Control);	
			}
		}

		private void ImageLoadingFinished(CachedImage element)
		{
			MainThreadDispatcher.Instance.Post(() =>
			{
				if (element != null && !_isDisposed)
				{
					((IVisualElementController)element).NativeSizeChanged();
					element.SetIsLoading(false);
				}
			});
		}

		private void ReloadImage()
		{
			SetImage(null);
		}

		private void Cancel()
		{
			var taskToCancel = _currentTask;
			if (taskToCancel != null && !taskToCancel.IsCancelled)
			{
				taskToCancel.Cancel();
			}
		}
			
		private Task<byte[]> GetImageAsJpgAsync(GetImageAsJpgArgs args)
		{
			return GetImageAsByteAsync(false, args.Quality, args.DesiredWidth, args.DesiredHeight);
		}

		private Task<byte[]> GetImageAsPngAsync(GetImageAsPngArgs args)
		{
			return GetImageAsByteAsync(true, 90, args.DesiredWidth, args.DesiredHeight);
		}

		private async Task<byte[]> GetImageAsByteAsync(bool usePNG, int quality, int desiredWidth, int desiredHeight)
		{
			UIImage image = null;

			await MainThreadDispatcher.Instance.PostAsync(() => {
				if (Control != null)
					image = Control.Image;
			}).ConfigureAwait(false);

			if (image == null)
				return null;

			if (desiredWidth != 0 || desiredHeight != 0)
			{
				image = image.ResizeUIImage((double)desiredWidth, (double)desiredHeight, InterpolationMode.Default);
			}

			NSData imageData = usePNG ? image.AsPNG() : image.AsJPEG((nfloat)quality / 100f);

			if (imageData == null || imageData.Length == 0)
				return null;

			var encoded = imageData.ToArray();
			imageData.Dispose();

			if (desiredWidth != 0 || desiredHeight != 0)
			{
				image.Dispose();
			}

			return encoded;	
		}
	}
}

