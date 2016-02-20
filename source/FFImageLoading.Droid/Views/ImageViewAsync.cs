using System;
using Android.Content;
using Android.Util;
using System.Drawing;
using FFImageLoading.Extensions;
using Android.Runtime;

namespace FFImageLoading.Views
{
	[Register("ffimageloading.views.ImageViewAsync")]
	public class ImageViewAsync : ManagedImageView
	{
		protected SizeF? _predefinedSize;

		public ImageViewAsync(IntPtr javaReference, JniHandleOwnership transfer)
			: base(javaReference, transfer)
		{
			SetWillNotDraw(false);
		}

		public ImageViewAsync(Context context, SizeF? predefinedSize = null)
			: base(context)
		{
			SetWillNotDraw(false);
		}

		public ImageViewAsync(Context context, IAttributeSet attrs, SizeF? predefinedSize)
			: base(context, attrs)
		{
			SetWillNotDraw(false);
		}

		public ImageViewAsync(Context context, IAttributeSet attrs)
			: base(context, attrs)
		{
			SetWillNotDraw(false);
		}

		/* FMT: this is not fine when working with RecyclerView... It can detach and cache the view, then reattach it
		protected override void OnDetachedFromWindow()
		{
			CancelLoading();
			base.OnDetachedFromWindow();
		}*/

		public void CancelLoading()
		{
			ImageService.CancelWorkFor(this.GetImageLoaderTask());
		}
	}
}