using System;

namespace FFImageLoading
{
    public interface IDecodedImage<TNativeImageContainer>
    {
        bool IsAnimated { get; }

        TNativeImageContainer Image { get; set; }

        IAnimatedImage<TNativeImageContainer>[] AnimatedImages { get; }
    }
}
