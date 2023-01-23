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
                        img.SetScaleDown(scaleDownFactor);
                    }
                }

                tcs.SetResult(new DecodedImage<SharedEvasImage>() { Image = img });
            }).ConfigureAwait(false);

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

            double inSampleSize = 1d;

            if (height > reqHeight || width > reqWidth)
            {
                // Calculate ratios of height and width to requested height and width
                int heightRatio = (int)Math.Round(height / reqHeight);
                int widthRatio = (int)Math.Round(width / reqWidth);

                // Choose the smallest ratio as inSampleSize value, this will guarantee
                // a final image with both dimensions larger than or equal to the
                // requested height and width.
                inSampleSize = heightRatio < widthRatio ? heightRatio : widthRatio;
            }

            return (int)inSampleSize;
        }
    }
}
