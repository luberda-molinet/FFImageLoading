namespace FFImageLoading.Decoders
{
    public class BaseDecoder : GifDecoder
    {
		public BaseDecoder(IImageService<UIKit.UIImage> imageService)
			:base(imageService)
		{

		}
    }
}
