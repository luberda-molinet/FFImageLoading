// 111(c) Andrei Misiukevich
using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using FFImageLoading.Extensions;

namespace FFImageLoading.Decoders
{
    public class XmlDecoder : BaseDecoder
    {
        private readonly Lazy<ContextWrapper> _lazyContext = new Lazy<ContextWrapper>(() => new ContextWrapper(Android.App.Application.Context));
        protected ContextWrapper Context => _lazyContext.Value;

        public override Task<IDecodedImage<Bitmap>> DecodeAsync(Stream imageData, string path, Work.ImageSource source, Work.ImageInformation imageInformation, Work.TaskParameter parameters)
        {
            imageInformation.SetOriginalSize(parameters.WidthRequest, parameters.HeightRequest);
            var bitmap = TryCreateBitmapFromXml(imageInformation.AndroidResourceId, parameters);
            if(bitmap != null)
            {
                imageInformation.SetCurrentSize(bitmap.Width, bitmap.Height);
            }
            IDecodedImage<Bitmap> result = new DecodedImage<Bitmap> { Image = bitmap };
            return Task.FromResult(result);
        }

        private Bitmap TryCreateBitmapFromXml(int resId, Work.TaskParameter parameters)
        {
            if (resId == 0)
            {
                return null;
            }

            using (var drawable = Context.GetDrawable(resId))
            {
                if (!(drawable is VectorDrawable))
                {
                    return null;
                }
                var size = GetSize(drawable, parameters);
                var bitmap = Bitmap.CreateBitmap(size.Item1, size.Item2, Bitmap.Config.Argb8888);
                using (var canvas = new Canvas(bitmap))
                {
                    drawable.SetBounds(0, 0, canvas.Width, canvas.Height);
                    drawable.Draw(canvas);
                }
                return bitmap;
            }
        }

        private Tuple<int, int> GetSize(Drawable drawable, Work.TaskParameter parameters)
        {
            double sizeX = 0;
            double sizeY = 0;

            if (parameters.WidthRequest == 0 && parameters.HeightRequest == 0)
            {
                if (drawable.IntrinsicWidth > 0)
                {
                    sizeX = drawable.IntrinsicWidth;
                }
                else
                {
                    sizeX = 300;
                }

                if (drawable.IntrinsicHeight > 0)
                {
                    sizeY = drawable.IntrinsicHeight;
                }
                else
                {
                    sizeY = 300;
                }
            }
            else if (parameters.WidthRequest > 0 && parameters.HeightRequest > 0)
            {
                sizeX = parameters.WidthRequest;
                sizeY = parameters.HeightRequest;
            }
            else if (parameters.WidthRequest > 0)
            {
                sizeX = parameters.WidthRequest;
                sizeY = (parameters.WidthRequest / drawable.IntrinsicWidth) * drawable.IntrinsicHeight;
            }
            else
            {
                sizeX = (parameters.HeightRequest / drawable.IntrinsicHeight) * drawable.IntrinsicWidth;
                sizeY = parameters.HeightRequest;
            }

            if (parameters.DownSampleUseDipUnits)
            {
                sizeX = sizeX.DpToPixels();
                sizeY = sizeY.DpToPixels();
            }

            return new Tuple<int, int>((int)sizeX, (int)sizeY);
        }
    }
}
