using System;
using FFImageLoading.Work;
using Android.Content.Res;
using Android.Graphics;
using Android.Runtime;
using Android.Graphics.Drawables;

namespace FFImageLoading.Drawables
{
	public class SelfDisposingAsyncDrawable : SelfDisposingBitmapDrawable, IAsyncDrawable
	{
		private readonly WeakReference<ImageLoaderTask> _imageLoaderTaskReference;

		public SelfDisposingAsyncDrawable(Resources res, Bitmap bitmap, ImageLoaderTask imageLoaderTask)
			: base(res, bitmap)
		{
			if (imageLoaderTask == null)
				throw new ArgumentNullException("Parameter 'imageLoaderTask' cannot be null");
			
			_imageLoaderTaskReference = new WeakReference<ImageLoaderTask>(imageLoaderTask);
		}

		public SelfDisposingAsyncDrawable(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

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

