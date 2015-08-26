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
		/// Used for registration with dependency service
		/// </summary>
		public async static void Init()
		{
			var temp = DateTime.Now;
		}

		private bool isDisposed;

		public CachedImageRenderer()
		{
			AutoPackage = false;
		}

		protected override void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				isDisposed = true;
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

			UpdateBitmap(e.OldElement);
			UpdateAspect();
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);
			if (e.PropertyName == Image.SourceProperty.PropertyName)
			{
				UpdateBitmap(null);
			}
			if (e.PropertyName == Image.AspectProperty.PropertyName)
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

		private void UpdateBitmap(Image previous = null)
		{
			if (previous == null || !object.Equals(previous.Source, Element.Source))
			{
				Xamarin.Forms.ImageSource source = Element.Source;

				((IElementController)Element).SetValueFromRenderer(CachedImage.IsLoadingPropertyKey, true);

				CachedImageView formsImageView = Control as CachedImageView;
				if (formsImageView != null)
				{
					formsImageView.SkipInvalidate();
				}

				if (Element != null && object.Equals(Element.Source, source) && !isDisposed)
				{
					TaskParameter imageLoader = null;

					if (source is UriImageSource)
					{
						var urlSource = (UriImageSource)source;
						imageLoader = ImageService.LoadUrl(urlSource.Uri.ToString(), Element.CacheDuration);
					}
					else if (source is FileImageSource)
					{
						var fileSource = (FileImageSource)source;
						Control.SetImageDrawable(Context.Resources.GetDrawable(fileSource.File));
						ImageLoadingFinished(Element);
					}
					else if (source == null)
					{
						Control.SetImageDrawable(null);
						ImageLoadingFinished(Element);
					}
					else
					{
						throw new NotImplementedException("ImageSource type not supported");
					}

					if (imageLoader != null)
					{
						if ((int)Element.DownsampleHeight != 0 || (int)Element.DownsampleWidth != 0)
						{
							if (Element.DownsampleHeight > Element.DownsampleWidth)
							{
								imageLoader.DownSample(height: (int)Element.DownsampleWidth);
							}
							else
							{
								imageLoader.DownSample(width: (int)Element.DownsampleHeight);
							}
						}

						if (Element.RetryCount > 0)
						{
							imageLoader.Retry(Element.RetryCount, Element.RetryDelay);
						}
							
						imageLoader.TransparencyChannel = Element.TransparencyEnabled;

						imageLoader.Finish((work) => ImageLoadingFinished(Element));
						imageLoader.Into(Control);	
					}
				}
			}
		}

		void ImageLoadingFinished(CachedImage element)
		{
			if (element != null && !isDisposed)
			{
				((IElementController)element).SetValueFromRenderer(CachedImage.IsLoadingPropertyKey, false);
				((IVisualElementController)element).NativeSizeChanged();	
			}
		}
	}
}

