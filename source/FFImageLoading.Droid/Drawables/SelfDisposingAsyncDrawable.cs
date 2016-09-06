using System;
using FFImageLoading.Work;
using Android.Content.Res;
using Android.Graphics;
using Android.Runtime;
using System.IO;

namespace FFImageLoading.Drawables
{
	public class SelfDisposingAsyncDrawable : SelfDisposingBitmapDrawable, IAsyncDrawable
	{
		private readonly WeakReference<ImageLoaderTask> _imageLoaderTaskReference;

		public SelfDisposingAsyncDrawable(Resources res, Bitmap bitmap, ImageLoaderTask imageLoaderTask)
			: base(res, bitmap)
		{
			if (imageLoaderTask == null)
                throw new ArgumentNullException(nameof(imageLoaderTask));
			
			_imageLoaderTaskReference = new WeakReference<ImageLoaderTask>(imageLoaderTask);
		}

        public SelfDisposingAsyncDrawable() : base()
        {
        }

        public SelfDisposingAsyncDrawable(Resources resources) : base(resources)
        {
        }

        public SelfDisposingAsyncDrawable(Resources resources, Stream stream) : base(resources, stream)
        {
        }

        public SelfDisposingAsyncDrawable(Resources resources, string filePath) : base(resources, filePath)
        {
        }

        public SelfDisposingAsyncDrawable(Bitmap bitmap) : base(bitmap)
        {
        }

        public SelfDisposingAsyncDrawable(Stream stream) : base(stream)
        {
        }

        public SelfDisposingAsyncDrawable(string filePath) : base(filePath)
        {
        }

        public SelfDisposingAsyncDrawable(Resources resources, Bitmap bitmap) : base(resources, bitmap)
        {
        }

        public SelfDisposingAsyncDrawable(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
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

