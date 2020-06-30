using System;
using System.IO;
using System.Threading.Tasks;
using ElmSharp;
using AppFW = Tizen.Applications;

namespace FFImageLoading.Views
{
    public enum EvasImageAspect
    {
        /// <summary>
        /// Scale the image to fit the view. Some parts may be left empty (letter boxing).
        /// </summary>
        AspectFit = 0,

        /// <summary>
        /// Scale the image to fill the view. Some parts may be clipped in order to fill the view.
        /// </summary>
        AspectFill = 1,

        /// <summary>
        /// Scale the image so it exactly fills the view. Scaling may not be uniform in X and Y
        /// </summary>
        Fill = 2
    }
    public class EvasImageContainer : Box
    {
        EvasImageAspect _aspect = EvasImageAspect.AspectFit;
        EvasImage _content = null;
        SharedEvasImage _source = null;

        public EvasImageContainer(EvasObject parent) : base(parent)
        {
            _content = new EvasImage(parent);
            _content.IsFilled = true;
            _content.Show();
            PackEnd(_content);
            SetLayoutCallback(OnLayout);
        }

        public EvasImageAspect Aspect
        {
            get
            {
                return _aspect;
            }
            set
            {
                _aspect = value;
            }
        }

        public event EventHandler SourceUpdated;

        public SharedEvasImage Source
        {
            get => _source;
            set
            {
                if (_source == value)
                    return;

                value?.AddRef();
                _source?.RemoveRef();

                _source = value;
                _content.SetSource(value);
                OnLayout();
                SourceUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        public Task<byte[]> GetImageData(bool asPNG, int quality)
        {
            if (Source == null)
                return null;

            string flags = $"quality={quality}";
            var path = Path.Combine(AppFW.Application.Current.DirectoryInfo.Cache, ".tmp");
            var absPath = Path.GetFullPath(path);
            if (!Directory.Exists(absPath))
                Directory.CreateDirectory(absPath);

            var filename = Source.GetHashCode().ToString() + GetHashCode().ToString() + (asPNG ? ".png" : ".jpg");
            var file = Path.Combine(absPath, filename);

            // Readme : I can't know, EFL "evas_object_image_save" was safty for multithread, So, i did't run on worker-thread
            Source.Save(file, null, flags);

            TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>();
            Task.Run(() =>
            {
                try
                {
                    FileStream fs = new FileStream(file, FileMode.Open);
                    MemoryStream ms = new MemoryStream();
                    fs.CopyTo(ms);
                    fs.Close();

                    tcs.SetResult(ms.ToArray());
                    ms.TryDispose();
                    Directory.Delete(file);
                }
                catch(Exception e)
                {
                    tcs.SetException(e);
                }
            });

            return tcs.Task;
        }

        protected override void OnUnrealize()
        {
            if (_source != null)
            {
                _content.SetSource(null);
                _source.RemoveRef();
                _source = null;
            }
            base.OnUnrealize();
        }

        void OnLayout()
        {
            if (Source == null)
            {
                _content.Geometry = new Rect(0, 0, 0, 0);
                return;
            }

            var imageSize = Source.Size;
            if (Geometry.Width == 0 || imageSize.Width == 0)
            {
                return;
            }

            Rect contentBound = Geometry;
            double canvasRatio = Geometry.Height / (double)Geometry.Width;
            double imageRatio = imageSize.Height / (double)imageSize.Width;

            if (imageRatio > canvasRatio)
            {
                if (Aspect == EvasImageAspect.AspectFit)
                {
                    contentBound.Height = Geometry.Height;
                    double ratio = Geometry.Height / (double)imageSize.Height;
                    contentBound.Width = (int)Math.Round(imageSize.Width * ratio);
                    int diff = Geometry.Width - contentBound.Width;
                    contentBound.X += (diff / 2);
                }
                else
                {
                    contentBound.Width = Geometry.Width;
                    double ratio = Geometry.Width / (double)imageSize.Width;
                    contentBound.Height = (int)Math.Round(imageSize.Height * ratio);
                    int diff = contentBound.Height - Geometry.Height;
                    contentBound.Y -= (diff / 2);
                }
            }
            else
            {
                if (Aspect == EvasImageAspect.AspectFit)
                {
                    contentBound.Width = Geometry.Width;
                    double ratio = Geometry.Width / (double)imageSize.Width;
                    contentBound.Height = (int)Math.Round(imageSize.Height * ratio);
                    int diff = Geometry.Height - contentBound.Height;
                    contentBound.Y += (diff / 2);
                }
                else
                {
                    contentBound.Height = Geometry.Height;
                    double ratio = Geometry.Height / (double)imageSize.Height;
                    contentBound.Width = (int)Math.Round(imageSize.Width * ratio);
                    int diff = contentBound.Width - Geometry.Width;
                    contentBound.X -= (diff / 2);
                }
            }
            _content.Geometry = contentBound;
        }
    }
}
