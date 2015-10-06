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
using System.Collections.Generic;
using FFImageLoading.Forms.Transformations;
using Android.Content;

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
			var formsContext = Android.App.Application.Context;
			RegisterTransformation(typeof(CircleTransformation), new FFImageLoading.Transformations.CircleTransformation());
			RegisterTransformation(typeof(RoundedTransformation), new FFImageLoading.Transformations.RoundedTransformation(0));
			RegisterTransformation(typeof(GrayscaleTransformation), new FFImageLoading.Transformations.GrayscaleTransformation());
			RegisterTransformation(typeof(BlurredTransformation), new FFImageLoading.Transformations.BlurredTransformation(formsContext, 10));
			RegisterTransformation(typeof(SepiaTransformation), new FFImageLoading.Transformations.SepiaTransformation());
		}

		static Dictionary<Type, ITransformation> transformationsDict = new Dictionary<Type, ITransformation>();
		public static Dictionary<Type, ITransformation> TransformationsDict
		{
			get { return transformationsDict; }
		}

		public static void RegisterTransformation(Type formsTransformationType, ITransformation androidTransformation)
		{
			if (transformationsDict.ContainsKey(formsTransformationType))
				throw new InvalidOperationException(string.Format("{0} transformation is already registered", formsTransformationType));

			if (!typeof(IFormsTransformation).IsAssignableFrom(formsTransformationType))
				throw new ArgumentException(string.Format("{0} must implement IFormsTransformation interface", formsTransformationType));

			transformationsDict.Add(formsTransformationType, androidTransformation);
		}

		private bool _isDisposed;

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
			}

			UpdateBitmap(e.OldElement);
			UpdateAspect();
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
					UpdateBitmap(null);	
				}
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

				((IElementController)Element).SetValueFromRenderer(CachedImage.IsLoadingPropertyKey, true);

				CachedImageView formsImageView = Control as CachedImageView;
				if (formsImageView != null)
				{
					formsImageView.SkipInvalidate();
				}
					
				if (Element != null && object.Equals(Element.Source, source) && !_isDisposed)
				{
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
									ITransformation nativeTransformation;
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

