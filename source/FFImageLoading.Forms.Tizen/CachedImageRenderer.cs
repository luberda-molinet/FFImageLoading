using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Tizen;
using FFImageLoading.Extensions;
using FFImageLoading.Forms.Args;
using FFImageLoading.Work;
using FFImageLoading.Views;
using Tizen.Applications;

namespace FFImageLoading.Forms.Platform
{
    public class CachedImageRenderer : ViewRenderer<CachedImage, EvasImageContainer>, IVisualElementRenderer
    {
        [RenderWith(typeof(CachedImageRenderer))]
        internal class _CachedImageRenderer
        {
        }

        IScheduledWork _currentTask;

        IVisualElementController ElementController => Element as IVisualElementController;

        public static void Init(FormsApplication application)
        {
            CachedImage.IsRendererInitialized = true;
            ImageService.MainWindowProvider = () => application.MainWindow;
        }

        SizeRequest IVisualElementRenderer.GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            Size request = new Size(0, 0);

            if (Control.Source != null)
            {
                request = CalculateAspectedSize(widthConstraint, heightConstraint, Control.Source.Size.ToDP());
            }
            return new SizeRequest(request, MinimumSize());
        }

        protected override void OnElementChanged(ElementChangedEventArgs<CachedImage> e)
        {
            if (Control == null)
            {
                SetNativeControl(new EvasImageContainer(ImageService.MainWindowProvider()));
                Control.SourceUpdated += OnSourceUpdated;
            }

            if (e.OldElement != null)
            {
                e.OldElement.InternalReloadImage = null;
                e.OldElement.InternalCancel = null;
                e.OldElement.InternalGetImageAsJPG = null;
                e.OldElement.InternalGetImageAsPNG = null;
            }

            if (e.NewElement != null)
            {
                e.NewElement.InternalReloadImage = new Action(UpdateImage);
                e.NewElement.InternalCancel = new Action(CancelIfNeeded);
                e.NewElement.InternalGetImageAsJPG = new Func<GetImageAsJpgArgs, Task<byte[]>>(GetImageAsJpgAsync);
                e.NewElement.InternalGetImageAsPNG = new Func<GetImageAsPngArgs, Task<byte[]>>(GetImageAsPngAsync);
            }

            base.OnElementChanged(e);
            UpdateImage();
            UpdateAspect();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Element.Source))
            {
                UpdateImage();
            }
            else if (e.PropertyName == nameof(Element.Aspect))
            {
                UpdateAspect();
            }
            base.OnElementPropertyChanged(sender, e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Control != null)
                {
                    Control.SourceUpdated -= OnSourceUpdated;
                }
                Element.InternalReloadImage = null;
                Element.InternalCancel = null;
                Element.InternalGetImageAsJPG = null;
                Element.InternalGetImageAsPNG = null;
            }
            base.Dispose(disposing);
        }

        Task<byte[]> GetImageAsPngAsync(GetImageAsPngArgs args)
        {
            return Control.GetImageData(true, 100);
        }

        Task<byte[]> GetImageAsJpgAsync(GetImageAsJpgArgs args)
        {
            return Control.GetImageData(false, args.Quality);
        }

        void UpdateAspect()
        {
            switch (Element.Aspect) {
                case Aspect.AspectFit:
                    Control.Aspect = EvasImageAspect.AspectFit;
                    break;
                case Aspect.AspectFill:
                    Control.Aspect = EvasImageAspect.AspectFill;
                    break;
                case Aspect.Fill:
                    Control.Aspect = EvasImageAspect.Fill;
                    break;
            }
        }

        void UpdateImage()
        {
            if (IsDisposed)
                return;

            CancelIfNeeded();

            var ffSource = ImageSourceBinding.GetImageSourceBinding(Element.Source, Element);
            if (ffSource == null)
            {
                Control.Source = null;
                return;
            }

            Element.SetIsLoading(true);
            Control.Source = null;

            var placeholderSource = ImageSourceBinding.GetImageSourceBinding(Element.LoadingPlaceholder, Element);
            var errorPlaceholderSource = ImageSourceBinding.GetImageSourceBinding(Element.ErrorPlaceholder, Element);

            TaskParameter imageLoader;
            Element.SetupOnBeforeImageLoading(out imageLoader, ffSource, placeholderSource, errorPlaceholderSource);
            if (imageLoader != null)
            {
                var finishAction = imageLoader.OnFinish;
                var sucessAction = imageLoader.OnSuccess;

                imageLoader.Success((imageInformation, loadingResult) =>
                {
                    sucessAction?.Invoke(imageInformation, loadingResult);
                });

                imageLoader.Finish((work) =>
                {
                    finishAction?.Invoke(work);
                    OnLoadFinished();
                });

                _currentTask = imageLoader.Into(Control);
            }
        }

        void OnLoadFinished()
        {
            ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() =>
            {
                if (!IsDisposed)
                {
                    Element.SetIsLoading(false);
                }
            }).ConfigureAwait(false);
        }

        void OnSourceUpdated(object sender, EventArgs e)
        {
            if (!IsDisposed)
            {
                ElementController?.NativeSizeChanged();
            }
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

        static Size CalculateAspectedSize(double widthConstraint, double heightConstraint, Size original)
        {
            double widthRatio = 1.0;
            double heightRatio = 1.0;
            if (original.Width > widthConstraint)
            {
                widthRatio = widthConstraint / original.Width;
                original.Width = widthConstraint;
            }
            if (original.Height > heightConstraint)
            {
                heightRatio = heightConstraint / original.Height;
                original.Height = heightConstraint;
            }

            if (widthRatio < heightRatio)
            {
                original.Height *= widthRatio;
            }
            else
            {
                original.Width *= heightRatio;
            }
            return original;
        }
    }
}