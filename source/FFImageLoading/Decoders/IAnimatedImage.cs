using System;

namespace FFImageLoading
{
    public interface IAnimatedImage<TNativeImageContainer>
    {
        int Delay { get; set; }

        TNativeImageContainer Image { get; set; }
    }
}
