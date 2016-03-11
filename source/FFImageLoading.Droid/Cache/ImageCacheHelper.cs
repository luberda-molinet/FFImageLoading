using FFImageLoading.Helpers;

namespace FFImageLoading.Cache
{
	/// <summary>
	/// Image cache Helper
	/// </summary>
	public class ImageCacheHelper : IImageCacheHelper
	{
		/// <summary>
		/// 
		/// </summary>
		public void Invalidate()
		{
			ImageService.Instance.InvalidateMemoryCache();
		}
	}
}