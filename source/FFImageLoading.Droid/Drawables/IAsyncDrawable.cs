using System;
using FFImageLoading.Work;

namespace FFImageLoading.Drawables
{
	public interface IAsyncDrawable
	{
		ImageLoaderTask GetImageLoaderTask();
	}
}

