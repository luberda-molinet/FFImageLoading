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
using Android.Views;
using System.Reflection;
using Android.Content;
using AView = Android.Views.View;

namespace FFImageLoading.Forms.Droid
{
    /// <summary>
    /// CachedImage Implementation
    /// </summary>
    [Preserve(AllMembers = true)]
    public class CachedImageFastRenderer : CachedImageView, IVisualElementRenderer
    {
        static Type _elementRendererType = typeof(ImageRenderer).Assembly.GetType("Xamarin.Forms.Platform.Android.FastRenderers.VisualElementRenderer");
        bool _isDisposed;
        CachedImage _element;
        int? _defaultLabelFor;
        VisualElementTracker _visualElementTracker;
        IDisposable _visualElementRenderer;
        IScheduledWork _currentTask;
        ImageSourceBinding _lastImageSource;
        readonly CachedImageRenderer.MotionEventHelper _motionEventHelper = new CachedImageRenderer.MotionEventHelper();

        public CachedImageFastRenderer(Context context) : base(context)
        {
        }

        [Obsolete("This constructor is obsolete as of version 3.0. Please use ImageRenderer(Context) instead.")]
        public CachedImageFastRenderer() : base(Xamarin.Forms.Forms.Context)
        {
        }

        public CachedImageFastRenderer(IntPtr javaReference, JniHandleOwnership transfer) : base(Xamarin.Forms.Forms.Context)
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
                        _visualElementTracker.Dispose();
                        _visualElementTracker = null;
                    }

                    if (_visualElementRenderer != null)
                    {
                        _visualElementRenderer.Dispose();
                        _visualElementRenderer = null;
                    }

                    if (_element != null)
                    {
                        _element.PropertyChanged -= OnElementPropertyChanged;

                        if (Platform.GetRenderer(_element) == this)
                        {
                            Platform.SetRenderer(_element, null);
                        }
                    }
                }
            }

            base.Dispose(disposing);
        }

        void OnElementChanged(ElementChangedEventArgs<CachedImage> e)
        {
            this.EnsureId();

            // TODO Xamarin-Internal class - Is it necessary? 
            // ElevationHelper.SetElevation(this, e.NewElement);
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
            if (base.OnTouchEvent(e))
                return true;

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

            CachedImage oldElement = _element;
            _element = image;

            if (oldElement != null)
                oldElement.PropertyChanged -= OnElementPropertyChanged;

            element.PropertyChanged += OnElementPropertyChanged;

            if (_visualElementTracker == null)
                _visualElementTracker = new VisualElementTracker(this);

            if (_visualElementRenderer == null)
            {
                _visualElementRenderer = (IDisposable)Activator.CreateInstance(_elementRendererType, this);
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
                //var finishAction = imageLoader.OnFinish;
                var sucessAction = imageLoader.OnSuccess;

                //imageLoader.Finish((work) =>
                //{
                //    finishAction?.Invoke(work);
                //    // ImageLoadingFinished(image);
                //});

                imageLoader.Success((imageInformation, loadingResult) =>
                {
                    sucessAction?.Invoke(imageInformation, loadingResult);
                    _lastImageSource = ffSource;
                });

                _currentTask = imageLoader.Into(imageView);
            }
        }

        //async void ImageLoadingFinished(CachedImage element)
        //{
        //    await ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() =>
        //    {
        //        if (element != null && !_isDisposed)
        //        {
        //            ((IVisualElementController)element).NativeSizeChanged();
        //            element.SetIsLoading(false);
        //        }
        //    });
        //}

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
    }
}
