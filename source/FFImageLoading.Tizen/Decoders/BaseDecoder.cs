using System;
using System.IO;
using System.Threading.Tasks;
using ElmSharp;
using FFImageLoading.Extensions;
using FFImageLoading.Views;
using FFImageLoading.Work;

namespace FFImageLoading.Decoders
{
    public class BaseDecoder : IDecoder<SharedEvasImage>
    {
        public Task<IDecodedImage<SharedEvasImage>> DecodeAsync(Stream imageData, string path, ImageSource source, ImageInformation imageInformation, TaskParameter parameters)
        {
            if (imageData == null)
                throw new ArgumentNullException(nameof(imageData));
            
            TaskCompletionSource<IDecodedImage<SharedEvasImage>> tcs = new TaskCompletionSource<IDecodedImage<SharedEvasImage>>();

            ImageService.Instance.Config.MainThreadDispatcher.PostAsync(() =>
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
                if (parameters.DownSampleSize != null && (parameters.DownSampleSize.Item1 > 0 || parameters.DownSampleSize.Item2 > 0))
                {
                    // Calculate inSampleSize
                    int downsampleWidth = parameters.DownSampleSize.Item1;
                    int downsampleHeight = parameters.DownSampleSize.Item2;

                    if (parameters.DownSampleUseDipUnits)
                    {
                        downsampleWidth = downsampleWidth.DpToPixels();
                        downsampleHeight = downsampleHeight.DpToPixels();
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

                tcs.SetResult(new DecodedImage<SharedEvasImage>() { Image = img });
            });

            return tcs.Task;
        }

        public EvasObject MainWindow
        {
            get
            {
                return ImageService.MainWindowProvider?.Invoke() ?? null;
            }
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
}
