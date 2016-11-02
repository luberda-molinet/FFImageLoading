//using Android.Widget;
//using FFImageLoading.Work;
//using System;
//using FFImageLoading.Drawables;

//namespace FFImageLoading.Extensions
//{
//	public static class ImageViewExtensions
//	{
//		/// <summary>
//		/// Retrieve the currently active work task (if any) associated with this imageView.
//		/// </summary>
//		/// <param name="imageView"></param>
//		/// <returns></returns>
//		public static IImageLoaderTask GetImageLoaderTask(this ImageView imageView)
//		{
//			if (imageView == null || imageView.Handle == IntPtr.Zero)
//				return null;
			
//			var drawable = imageView.Drawable;

//			var asyncDrawable = drawable as IAsyncDrawable;
//			if (asyncDrawable != null)
//			{
//				return asyncDrawable.GetImageLoaderTask();
//			}

//			return null;
//		}
//	}
//}