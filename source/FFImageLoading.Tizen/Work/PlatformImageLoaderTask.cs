using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using FFImageLoading.Cache;
using FFImageLoading.Views;
using ElmSharp;

namespace FFImageLoading.Work
{
    public class PlatformImageLoaderTask<TImageView> : ImageLoaderTask<SharedEvasImage, TImageView> where TImageView : class
    {
        public PlatformImageLoaderTask(ITarget<SharedEvasImage, TImageView> target, TaskParameter parameters, IImageService imageService) : base(EvasImageCache.Instance, target, parameters, imageService)
        {
        }

        public EvasObject MainWindow
        {
            get
            {
                return FFImageLoading.ImageService.MainWindowProvider?.Invoke() ?? null;
            }
        }

        protected override int DpiToPixels(int size)
        {
            return FFImageLoading.ImageService.DpToPixels(size);
        }

        protected override Task<SharedEvasImage> GenerateImageAsync(string path, ImageSource source, Stream imageData, ImageInformation imageInformation, bool enableTransformations, bool isPlaceholder)
        {
            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));

            ThrowIfCancellationRequested();

            TaskCompletionSource<SharedEvasImage> tcs = new TaskCompletionSource<SharedEvasImage>();

            MainThreadDispatcher.PostAsync(() =>
            {
                SharedEvasImage img = new SharedEvasImage(MainWindow);
                img.IsFilled = true;
                img.Show();
                img.SetStream(imageData);
                imageData.TryDispose();

                img.AddRef();
                EcoreMainloop.AddTimer(1.0, () => {
                    img.RemoveRef();
                    return false;
                });

                imageInformation.SetOriginalSize(img.Size.Width, img.Size.Height);
                imageInformation.SetCurrentSize(img.Size.Width, img.Size.Height);

                // DOWNSAMPLE
                if (Parameters.DownSampleSize != null && (Parameters.DownSampleSize.Item1 > 0 || Parameters.DownSampleSize.Item2 > 0))
                {
                    // Calculate inSampleSize
                    int downsampleWidth = Parameters.DownSampleSize.Item1;
                    int downsampleHeight = Parameters.DownSampleSize.Item2;

                    if (Parameters.DownSampleUseDipUnits)
                    {
                        downsampleWidth = DpiToPixels(downsampleWidth);
                        downsampleHeight = DpiToPixels(downsampleHeight);
                    }

                    int scaleDownFactor = CalculateScaleDownFactor(img.Size.Width, img.Size.Height, downsampleWidth, downsampleHeight);

                    if (scaleDownFactor > 1)
                    {
                        //System.//Console.WriteLine("GenerateImageAsync:: DownSample with {0}", scaleDownFactor);
                        imageInformation.SetCurrentSize(
                            (int)((double)img.Size.Width / scaleDownFactor),
                            (int)((double)img.Size.Height / scaleDownFactor));
                        EvasInterop.evas_object_image_load_scale_down_set(img.RealHandle, scaleDownFactor);
                    }
                }
                tcs.SetResult(img);
            });
            return tcs.Task;
        }

        protected override Task SetTargetAsync(SharedEvasImage image, bool animated)
        {
            if (Target == null)
                return Task.FromResult(true);

            return MainThreadDispatcher.PostAsync(() =>
            {
                ThrowIfCancellationRequested();
                PlatformTarget.Set(this, image, animated);
            });
        }
        
        public static int CalculateScaleDownFactor(int originWidth, int originHeight, int reqWidth, int reqHeight)
        {
            // Raw height and width of image
            float height = originHeight;
            float width = originWidth;

            if (reqWidth == 0)
                reqWidth = (int)((reqHeight / height) * width);

            if (reqHeight == 0)
                reqHeight = (int)((reqWidth / width) * height);

            double inSampleSize = 1D;

            if (height > reqHeight || width > reqWidth)
            {
                int halfHeight = (int)(height / 2);
                int halfWidth = (int)(width / 2);

                // Calculate a inSampleSize that is a power of 2 - the decoder will use a value that is a power of two anyway.
                while ((halfHeight / inSampleSize) > reqHeight && (halfWidth / inSampleSize) > reqWidth)
                {
                    inSampleSize *= 2;
                }
            }
            return (int)inSampleSize;
        }
    }

    static class EvasInterop
    {
        [DllImport("libevas.so.1")]
        internal static extern void evas_object_image_load_scale_down_set(IntPtr obj, int scale);
    }
}
