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
            var errorPlaceholderSource = ImageSourceBinding.GetImageSourceBinding(Element.ErrorPlaceholder, Element);

			if (oldElement != null && _lastImageSource != null && ffSource != null && !ffSource.Equals(_lastImageSource)
				&& (string.IsNullOrWhiteSpace(placeholderSource?.Path) || placeholderSource?.Stream != null))
			{
				_lastImageSource = null;
				Control.Image = null;
			}

			Element.SetIsLoading(true);

			Cancel();

            if (ffSource == null)
            {
                if (Control != null)
                    Control.Image = null;

                ImageLoadingFinished(Element);
            }
            else
            {
                var element = Element;
                TaskParameter imageLoader = null;
                element.SetupOnBeforeImageLoading(out imageLoader, ffSource, placeholderSource, errorPlaceholderSource);

                if (imageLoader != null)
                {
                    var finishAction = imageLoader.OnFinish;
                    var sucessAction = imageLoader.OnSuccess;

                    imageLoader.Finish((work) => {
                        finishAction?.Invoke(work);
                        ImageLoadingFinished(element);
                    });

                    imageLoader.Success((imageInformation, loadingResult) =>
                    {
                        sucessAction?.Invoke(imageInformation, loadingResult);
                        _lastImageSource = ffSource;
                    });

                    _currentTask = imageLoader.Into(Control);
                }
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

