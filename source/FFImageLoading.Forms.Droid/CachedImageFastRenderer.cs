using Android.Widget;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using FFImageLoading.Work;
using FFImageLoading.Forms.Platform;
using FFImageLoading.Forms;
using Android.Runtime;
using Android.Graphics.Drawables;
using Android.Graphics;
using System.IO;
using System.Threading.Tasks;
using FFImageLoading.Forms.Args;
using FFImageLoading.Helpers;
using FFImageLoading.Views;
using Android.Views;
using System.Reflection;
using Android.Content;
using Android.OS;
using AView = Android.Views.View;

namespace FFImageLoading.Forms.Platform
{
    /// <summary>
    /// CachedImage Implementation
    /// </summary>
    [Preserve(AllMembers = true)]
    public class CachedImageFastRenderer : CachedImageView, IVisualElementRenderer
    {
        internal static readonly Type ElementRendererType = typeof(ImageRenderer).Assembly.GetType("Xamarin.Forms.Platform.Android.FastRenderers.VisualElementRenderer");
        private static readonly MethodInfo _viewExtensionsMethod = typeof(ImageRenderer).Assembly.GetType("Xamarin.Forms.Platform.Android.ViewExtensions")?.GetRuntimeMethod("EnsureId", new[] { typeof(Android.Views.View) });
        private static readonly MethodInfo _elementRendererTypeOnTouchEvent = ElementRendererType?.GetRuntimeMethod("OnTouchEvent", new[] { typeof(MotionEvent) });
        private bool _isDisposed;
		private int? _defaultLabelFor;
        private VisualElementTracker _visualElementTracker;
        private IDisposable _visualElementRenderer;
        private IScheduledWork _currentTask;
        private ImageSourceBinding _lastImageSource;
        private readonly CachedImageRenderer.MotionEventHelper _motionEventHelper = new CachedImageRenderer.MotionEventHelper();
        private readonly object _updateBitmapLock = new object();

        [Obsolete("This constructor is obsolete as of version 2.5. Please use CachedImageFastRenderer(Context) instead.")]
        public CachedImageFastRenderer() : base(Xamarin.Forms.Forms.Context)
        {
        }

        public CachedImageFastRenderer(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CachedImageFastRenderer(Context context) : base(context)
        {
        }

        public CachedImageFastRenderer(Context context, Android.Util.IAttributeSet attrs): base(context, attrs)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                CancelIfNeeded();

                if (disposing)
                {
                    if (_visualElementTracker != null)
                    {
                        _visualElementTracker.TryDispose();
                        _visualElementTracker = null;
                    }

                    if (_visualElementRenderer != null)
                    {
                        _visualElementRenderer.TryDispose();
                        _visualElementRenderer = null;
                    }

                    if (TypedElement != null)
                    {
                        TypedElement.PropertyChanged -= OnElementPropertyChanged;

                        if (Xamarin.Forms.Platform.Android.Platform.GetRenderer(TypedElement) == this)
                        {
                            Xamarin.Forms.Platform.Android.Platform.SetRenderer(TypedElement, null);
                        }
                    }
                }
            }

            base.Dispose(disposing);
        }

        private void OnElementChanged(ElementChangedEventArgs<CachedImage> e)
        {
            _viewExtensionsMethod.Invoke(null, new[] { this });

            ElevationHelper.SetElevation(this, e.NewElement);
            ElementChanged?.Invoke(this, new VisualElementChangedEventArgs(e.OldElement, e.NewElement));

            if (e.OldElement != null)
            {
                e.OldElement.InternalReloadImage = null;
                e.OldElement.InternalCancel = null;
                e.OldElement.InternalGetImageAsJPG = null;
                e.OldElement.InternalGetImageAsPNG = null;
            }

            if (e.NewElement != null)
            {
				e.NewElement.InternalReloadImage = new Action(ReloadImage);
                e.NewElement.InternalCancel = new Action(CancelIfNeeded);
                e.NewElement.InternalGetImageAsJPG = new Func<GetImageAsJpgArgs, Task<byte[]>>(GetImageAsJpgAsync);
                e.NewElement.InternalGetImageAsPNG = new Func<GetImageAsPngArgs, Task<byte[]>>(GetImageAsPngAsync);

                _motionEventHelper?.UpdateElement(e.NewElement);
                SetBackgroundColor(e.NewElement.BackgroundColor);
                UpdateBitmap(Control, TypedElement, e.OldElement);
                UpdateAspect();
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (_elementRendererTypeOnTouchEvent != null && (bool)_elementRendererTypeOnTouchEvent.Invoke(_visualElementRenderer, new[] { e }))
            {
                return true;
            }
            if (base.OnTouchEvent(e))
            {
                return true;
            }

            return CachedImage.FixedAndroidMotionEventHandler && _motionEventHelper.HandleMotionEvent(Parent, e);
        }

        private Size MinimumSize()
        {
            return new Size();
        }

        SizeRequest IVisualElementRenderer.GetDesiredSize(int widthConstraint, int heightConstraint)
        {
            if (_isDisposed)
            {
                return new SizeRequest();
            }

            Measure(widthConstraint, heightConstraint);
            return new SizeRequest(new Size(MeasuredWidth, MeasuredHeight), MinimumSize());
        }

        void IVisualElementRenderer.SetElement(VisualElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (!(element is CachedImage image))
                throw new ArgumentException("Element is not of type " + typeof(CachedImage), nameof(element));

            var oldElement = TypedElement;
            TypedElement = image;

            if (oldElement != null)
                oldElement.PropertyChanged -= OnElementPropertyChanged;

            element.PropertyChanged += OnElementPropertyChanged;

            if (_visualElementTracker == null)
                _visualElementTracker = new VisualElementTracker(this);

            if (_visualElementRenderer == null)
            {
                _visualElementRenderer = (IDisposable)Activator.CreateInstance(ElementRendererType, this);
            }

            _motionEventHelper.UpdateElement(element);
            OnElementChanged(new ElementChangedEventArgs<CachedImage>(oldElement, TypedElement));

            // TODO Xamarin-Internal class - Is it necessary? 
            //_element?.SendViewInitialized(Control);
        }

        void IVisualElementRenderer.SetLabelFor(int? id)
        {
            if (_defaultLabelFor == null)
                _defaultLabelFor = LabelFor;

            LabelFor = (int)(id ?? _defaultLabelFor);
        }

        void IVisualElementRenderer.UpdateLayout() => _visualElementTracker?.UpdateLayout();

        VisualElement IVisualElementRenderer.Element => TypedElement;

        CachedImage TypedElement { get; set; }

        VisualElementTracker IVisualElementRenderer.Tracker => _visualElementTracker;

        AView IVisualElementRenderer.View => this;

        ViewGroup IVisualElementRenderer.ViewGroup => null;

        CachedImageView Control => this;

        public event EventHandler<VisualElementChangedEventArgs> ElementChanged;
        public event EventHandler<PropertyChangedEventArgs> ElementPropertyChanged;

        protected virtual void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == CachedImage.SourceProperty.PropertyName)
            {
                UpdateBitmap(Control, TypedElement, null);
            }
            if (e.PropertyName == CachedImage.AspectProperty.PropertyName)
            {
                UpdateAspect();
            }
            if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
            {
                SetBackgroundColor(TypedElement.BackgroundColor);
            }

            ElementPropertyChanged?.Invoke(this, e);
        }

        private void SetBackgroundColor(Xamarin.Forms.Color color)
        {
            Control?.SetBackgroundColor(color.ToAndroid());
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);

            if ((int)Build.VERSION.SdkInt >= 18)
            {
                ClipBounds = GetScaleType() == ScaleType.CenterCrop ? new Rect(0, 0, right - left, bottom - top) : null;
            }                
        }

        private void UpdateAspect()
        {
            if (Control == null || Control.Handle == IntPtr.Zero || TypedElement == null || _isDisposed)
                return;

            if (TypedElement.Aspect == Aspect.AspectFill)
                Control.SetScaleType(ScaleType.CenterCrop);

            else if (TypedElement.Aspect == Aspect.Fill)
                Control.SetScaleType(ScaleType.FitXy);

            else
                Control.SetScaleType(ScaleType.FitCenter);
        }

        private void UpdateBitmap(CachedImageView imageView, CachedImage image, CachedImage previousImage)
        {
            lock (_updateBitmapLock)
            {
                CancelIfNeeded();

                if (image == null || imageView == null || imageView.Handle == IntPtr.Zero || _isDisposed)
                    return;

                var ffSource = ImageSourceBinding.GetImageSourceBinding(image.Source, image);
                if (ffSource == null)
                {
                    if (_lastImageSource == null)
                        return;

                    _lastImageSource = null;
                    imageView.SetImageResource(Android.Resource.Color.Transparent);
                    return;
                }

                if (previousImage != null && !ffSource.Equals(_lastImageSource))
                {
                    _lastImageSource = null;
                    imageView.SkipInvalidate();
                    Control.SetImageResource(Android.Resource.Color.Transparent);
                }

                image.SetIsLoading(true);

                var placeholderSource = ImageSourceBinding.GetImageSourceBinding(image.LoadingPlaceholder, image);
                var errorPlaceholderSource = ImageSourceBinding.GetImageSourceBinding(image.ErrorPlaceholder, image);
                image.SetupOnBeforeImageLoading(out var imageLoader, ffSource, placeholderSource, errorPlaceholderSource);

                if (imageLoader != null)
                {
                    var finishAction = imageLoader.OnFinish;
                    var sucessAction = imageLoader.OnSuccess;

                    imageLoader.Finish((work) =>
                    {
                        finishAction?.Invoke(work);
                        ImageLoadingSizeChanged(image, false);
                    });

                    imageLoader.Success((imageInformation, loadingResult) =>
                    {
                        sucessAction?.Invoke(imageInformation, loadingResult);
                        _lastImageSource = ffSource;
                    });

                    imageLoader.LoadingPlaceholderSet(() => ImageLoadingSizeChanged(image, true));

                    if (!_isDisposed)
                        _currentTask = imageLoader.Into(imageView);
                }
            }
        }

		private async void ImageLoadingSizeChanged(CachedImage element, bool isLoading)
		{
			if (element == null || _isDisposed)
				return;
				
			await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() =>
			{
				if (element == null || _isDisposed)
					return;

				((IVisualElementController)element).NativeSizeChanged();

				if (!isLoading)
					element.SetIsLoading(isLoading);
			}).ConfigureAwait(false);
		}

		private void ReloadImage()
        {
            UpdateBitmap(Control, TypedElement, null);
        }

        private void CancelIfNeeded()
        {
            try
            {
                var taskToCancel = _currentTask;
                if (taskToCancel != null && !taskToCancel.IsCancelled)
                {
                    taskToCancel.Cancel();
                }

                _currentTask = null;
            }
            catch { }
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
                
            if (!(Control.Drawable is BitmapDrawable drawable) || drawable.Bitmap == null)
                return null;

            var bitmap = drawable.Bitmap;

            if (desiredWidth != 0 || desiredHeight != 0)
            {
                var widthRatio = (double)desiredWidth / bitmap.Width;
                var heightRatio = (double)desiredHeight / bitmap.Height;
                var scaleRatio = Math.Min(widthRatio, heightRatio);

                if (desiredWidth == 0)
                    scaleRatio = heightRatio;

                if (desiredHeight == 0)
                    scaleRatio = widthRatio;

                var aspectWidth = (int)(bitmap.Width * scaleRatio);
                var aspectHeight = (int)(bitmap.Height * scaleRatio);

                bitmap = Bitmap.CreateScaledBitmap(bitmap, aspectWidth, aspectHeight, true);
            }

            using (var stream = new MemoryStream())
            {
                await bitmap.CompressAsync(format, quality, stream).ConfigureAwait(false);
                var compressed = stream.ToArray();

                if (desiredWidth != 0 || desiredHeight != 0)
                {
                    bitmap.Recycle();
                    bitmap.TryDispose();
                }

                return compressed;
            }
        }

        private static bool? _isLollipopOrNewer;
        internal static bool IsLollipopOrNewer
        {
            get
            {
                if (!_isLollipopOrNewer.HasValue)
                    _isLollipopOrNewer = (int)Build.VERSION.SdkInt >= 21;
                return _isLollipopOrNewer.Value;
            }
        }

        internal static class ElevationHelper
        {
            private static readonly MethodInfo _getEleveationMethod = typeof(Image).Assembly.GetType("Xamarin.Forms.PlatformConfiguration.AndroidSpecific.Elevation")?.GetRuntimeMethod("GetElevation", new Type[] { typeof(VisualElement) });

            internal static void SetElevation(AView view, VisualElement element)
            {
                if (_getEleveationMethod == null || view == null || element == null || !IsLollipopOrNewer)
                {
                    return;
                }

                var elevation = (float?)_getEleveationMethod.Invoke(null, new[] { element });

                if (elevation.HasValue)
                    view.Elevation = elevation.Value;
            }
        }
    }
}
