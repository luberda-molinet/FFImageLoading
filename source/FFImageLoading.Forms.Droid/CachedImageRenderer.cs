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
            var errorPlaceholderSource = ImageSourceBinding.GetImageSourceBinding(Element.ErrorPlaceholder, Element);

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

                if (ffSource == null)
                {
                    imageView.SetImageResource(global::Android.Resource.Color.Transparent);
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

                        imageLoader.Finish((work) =>
                        {
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

