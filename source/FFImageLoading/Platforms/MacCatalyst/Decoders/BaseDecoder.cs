using UIKit;

namespace FFImageLoading.Decoders
{
	public class BaseDecoder : GifDecoder
	{
		public BaseDecoder(IImageService<UIImage> imageService) : base(imageService)
		{
		}
	}
}
