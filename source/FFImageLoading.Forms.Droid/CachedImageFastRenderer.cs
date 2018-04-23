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
        static readonly MethodInfo _viewExtensionsMethod = typeof(ImageRenderer).Assembly.GetType("Xamarin.Forms.Platform.Android.ViewExtensions")?.GetRuntimeMethod("EnsureId", new[] { typeof(Android.Views.View) });
        static readonly MethodInfo _ElementRendererTypeOnTouchEvent = ElementRendererType?.GetRuntimeMethod("OnTouchEvent", new[] { typeof(MotionEvent) });

        bool _isSizeSet;
        bool _isDisposed;
        CachedImage _element;
        int? _defaultLabelFor;
        VisualElementTracker _visualElementTracker;
        IDisposable _visualElementRenderer;
        IScheduledWork _currentTask;
        ImageSourceBinding _lastImageSource;
        readonly CachedImageRenderer.MotionEventHelper _motionEventHelper = new CachedImageRenderer.MotionEventHelper();
        readonly object _updateBitmapLock = new object();

        [Obsolete("This constructor is obsolete as of version 2.5. Please use ImageRenderer(Context) instead.")]
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

                    if (_element != null)
                    {
                        _element.PropertyChanged -= OnElementPropertyChanged;

                        if (Xamarin.Forms.Platform.Android.Platform.GetRenderer(_element) == this)
                        {
                            Xamarin.Forms.Platform.Android.Platform.SetRenderer(_element, null);
                        }
                    }
                }
            }

            base.Dispose(disposing);
        }

        void OnElementChanged(ElementChangedEventArgs<CachedImage> e)
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
                UpdateBitmap(Control, TypedElement, e.OldElement);
                UpdateAspect();
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (_ElementRendererTypeOnTouchEvent != null && (bool)_ElementRendererTypeOnTouchEvent.Invoke(_visualElementRenderer, new[] { e }))
            {
                return true;
            }
            else if (base.OnTouchEvent(e))
            {
                return true;
            }

            return CachedImage.FixedAndroidMotionEventHandler ? _motionEventHelper.HandleMotionEvent(Parent, e) : false;
        }

        Size MinimumSize()
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

            var image = element as CachedImage;
            if (image == null)
                throw new ArgumentException("Element is not of type " + typeof(CachedImage), nameof(element));

            _isSizeSet = false;

            CachedImage oldElement = _element;
            _element = image;

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
            OnElementChanged(new ElementChangedEventArgs<CachedImage>(oldElement, _element));

            // TODO Xamarin-Internal class - Is it necessary? 
            // _element?.SendViewInitialized(Control);
        }

        void IVisualElementRenderer.SetLabelFor(int? id)
        {
            if (_defaultLabelFor == null)
                _defaultLabelFor = LabelFor;

            LabelFor = (int)(id ?? _defaultLabelFor);
        }

        void IVisualElementRenderer.UpdateLayout() => _visualElementTracker?.UpdateLayout();

        VisualElement IVisualElementRenderer.Element => _element;

        CachedImage TypedElement => _element;

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

            ElementPropertyChanged?.Invoke(this, e);
        }   

        void UpdateAspect()
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

        void UpdateBitmap(CachedImageView imageView, CachedImage image, CachedImage previousImage)
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
                    imageView.SetImageResource(global::Android.Resource.Color.Transparent);
                    return;
                }

                if (previousImage != null && !ffSource.Equals(_lastImageSource))
                {
                    _lastImageSource = null;
                    imageView.SkipInvalidate();
                    Control.SetImageResource(global::Android.Resource.Color.Transparent);
                }

                image.SetIsLoading(true);

                var placeholderSource = ImageSourceBinding.GetImageSourceBinding(image.LoadingPlaceholder, image);
                var errorPlaceholderSource = ImageSourceBinding.GetImageSourceBinding(image.ErrorPlaceholder, image);
                TaskParameter imageLoader;
                image.SetupOnBeforeImageLoading(out imageLoader, ffSource, placeholderSource, errorPlaceholderSource);

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

        async void ImageLoadingSizeChanged(CachedImage element, bool isLoading)
        {
            await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() =>
            {
                if (element != null && !_isDisposed)
                {
                    if (!isLoading || !_isSizeSet)
                    {
                        ((IVisualElementController)element).NativeSizeChanged();
                        _isSizeSet = true;
                    }

                    if (!isLoading)
                        element.SetIsLoading(isLoading);
                }
            });
        }

        void ReloadImage()
        {
            UpdateBitmap(Control, TypedElement, null);
        }

        void CancelIfNeeded()
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
            catch (Exception) { }
        }

        Task<byte[]> GetImageAsJpgAsync(GetImageAsJpgArgs args)
        {
            return GetImageAsByteAsync(Bitmap.CompressFormat.Jpeg, args.Quality, args.DesiredWidth, args.DesiredHeight);
        }

        Task<byte[]> GetImageAsPngAsync(GetImageAsPngArgs args)
        {
            return GetImageAsByteAsync(Bitmap.CompressFormat.Png, 90, args.DesiredWidth, args.DesiredHeight);
        }

        async Task<byte[]> GetImageAsByteAsync(Bitmap.CompressFormat format, int quality, int desiredWidth, int desiredHeight)
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
                    bitmap.TryDispose();
                }

                return compressed;
            }
        }

        static bool? s_isLollipopOrNewer;
        internal static bool IsLollipopOrNewer
        {
            get
            {
                if (!s_isLollipopOrNewer.HasValue)
                    s_isLollipopOrNewer = (int)Android.OS.Build.VERSION.SdkInt >= 21;
                return s_isLollipopOrNewer.Value;
            }
        }

        internal static class ElevationHelper
        {
            static readonly MethodInfo _getEleveationMethod = typeof(Image).Assembly.GetType("Xamarin.Forms.PlatformConfiguration.AndroidSpecific.Elevation")?.GetRuntimeMethod("GetElevation", new Type[] { typeof(VisualElement) });

            internal static void SetElevation(global::Android.Views.View view, VisualElement element)
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
