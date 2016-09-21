using System;
using FFImageLoading.Work;
using Android.Content.Res;
using Android.Graphics;
using Android.Runtime;
using Android.Graphics.Drawables;
using System.IO;

namespace FFImageLoading.Drawables
{
	public class AsyncDrawable : BitmapDrawable, IAsyncDrawable
	{
		private readonly WeakReference<ImageLoaderTask> _imageLoaderTaskReference;

		public AsyncDrawable(Resources res, Bitmap bitmap, ImageLoaderTask imageLoaderTask)
			: base(res, bitmap)
		{
			_imageLoaderTaskReference = new WeakReference<ImageLoaderTask>(imageLoaderTask);
		}

        public AsyncDrawable() : base()
        {
        }

        public AsyncDrawable(Resources resources) : base(resources)
        {
        }

        public AsyncDrawable(Resources resources, Stream stream) : base(resources, stream)
        {
        }

        public AsyncDrawable(Resources resources, string filePath) : base(resources, filePath)
        {
        }

        public AsyncDrawable(Bitmap bitmap) : base(bitmap)
        {
        }

        public AsyncDrawable(Stream stream) : base(stream)
        {
        }

        public AsyncDrawable(string filePath) : base(filePath)
        {
        }

        public AsyncDrawable(Resources resources, Bitmap bitmap) : base(resources, bitmap)
        {
        }

        public AsyncDrawable(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }

		public ImageLoaderTask GetImageLoaderTask()
		{
			if (_imageLoaderTaskReference == null)
				return null;

			ImageLoaderTask task;
			_imageLoaderTaskReference.TryGetTarget(out task);
			return task;
		}
	}
}

