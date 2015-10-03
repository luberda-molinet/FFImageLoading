using CoreGraphics;
using System;
using System.ComponentModel;
using UIKit;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using FFImageLoading.Work;
using FFImageLoading;
using Foundation;
using FFImageLoading.Forms;
using FFImageLoading.Forms.Touch;
using System.Collections.Generic;
using FFImageLoading.Forms.Transformations;

[assembly:ExportRenderer(typeof (CachedImage), typeof (CachedImageRenderer))]
namespace FFImageLoading.Forms.Touch
{
	/// <summary>
	/// CachedImage Implementation
	/// </summary>
	[Preserve(AllMembers = true)]
	public class CachedImageRenderer : ViewRenderer<CachedImage, UIImageView>
	{
		/// <summary>
		///   Used for registration with dependency service
		/// </summary>
		public static void Init()
		{
			RegisterTransformation(typeof(CircleTransformation), new FFImageLoading.Transformations.CircleTransformation());
			RegisterTransformation(typeof(RoundedTransformation), new FFImageLoading.Transformations.RoundedTransformation(0));
			RegisterTransformation(typeof(GrayscaleTransformation), new FFImageLoading.Transformations.GrayscaleTransformation());
		}

		static Dictionary<Type, IMultiplatformTransformation> transformationsDict = new Dictionary<Type, IMultiplatformTransformation>();
		public static Dictionary<Type, IMultiplatformTransformation> TransformationsDict
		{
			get { return transformationsDict; }
		}

		public static void RegisterTransformation(Type formsTransformationType, IMultiplatformTransformation iosTransformation)
		{
			if (transformationsDict.ContainsKey(formsTransformationType))
				throw new InvalidOperationException(string.Format("{0} transformation is already registered", formsTransformationType));

			if (!typeof(IFormsTransformation).IsAssignableFrom(formsTransformationType))
				throw new ArgumentException(string.Format("{0} must implement IFormsTransformation interface", formsTransformationType));

			transformationsDict.Add(formsTransformationType, iosTransformation);
		}

		private bool _isDisposed;

		protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
			{
				return;
			}

			UIImage image;
			if (disposing && base.Control != null && (image = base.Control.Image) != null)
			{
				image.Dispose();
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
			}
			base.OnElementChanged(e);
		}

		int fixLastCount = 0; // TODO TEMPORARY FIX (https://bugzilla.xamarin.com/show_bug.cgi?id=34531)
		ImageSourceBinding lastImageSource = null; // TODO TEMPORARY FIX (https://bugzilla.xamarin.com/show_bug.cgi?id=34531)
		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == CachedImage.SourceProperty.PropertyName)
			{
				fixLastCount++;

				var ffSource = ImageSourceBinding.GetImageSourceBinding(Element.Source);

				if (ffSource == null || !ffSource.Equals(lastImageSource) || fixLastCount > 1)
				{
					fixLastCount = 0;
					lastImageSource = ffSource;
					SetImage(null);
				}
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
			Xamarin.Forms.ImageSource source = base.Element.Source; 
			if (oldElement != null)
			{
				Xamarin.Forms.ImageSource source2 = oldElement.Source;
				if (object.Equals(source2, source))
				{
					return;
				}
				if (source2 is FileImageSource && source is FileImageSource && ((FileImageSource)source2).File == ((FileImageSource)source).File)
				{
					return;
				}
			}

			((IElementController)Element).SetValueFromRenderer(CachedImage.IsLoadingPropertyKey, true);

			TaskParameter imageLoader = null;

			var ffSource = ImageSourceBinding.GetImageSourceBinding(source);

			if (ffSource == null)
			{
				if (Control != null)
					Control.Image = null;
				
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
						imageLoader.DownSample(height: (int)Element.DownsampleWidth);
					}
					else
					{
						imageLoader.DownSample(width: (int)Element.DownsampleHeight);
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
					foreach (var transformation in Element.Transformations)
					{
						if (transformation != null)
						{
							IMultiplatformTransformation nativeTransformation;
							if (TransformationsDict.TryGetValue(transformation.GetType(), out nativeTransformation))
							{
								nativeTransformation.SetParameters(transformation.Parameters);
								imageLoader.Transform(nativeTransformation);		
							}
						}
					}
				}

				imageLoader.Finish((work) => ImageLoadingFinished(Element));
				imageLoader.Into(Control);	
			}
		}

		void ImageLoadingFinished(CachedImage element)
		{
			if (element != null && !_isDisposed)
			{
				((IElementController)element).SetValueFromRenderer(CachedImage.IsLoadingPropertyKey, false);
				((IVisualElementController)element).NativeSizeChanged();
			}
		}

	}
}

