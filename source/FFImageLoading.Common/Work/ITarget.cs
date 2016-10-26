using System;
using System.Threading.Tasks;

namespace FFImageLoading.Work
{
    public interface ITarget
    {
        bool IsValid { get; }

        bool IsTaskValid(IImageLoaderTask task);

        bool UsesSameNativeControl(IImageLoaderTask task);
    }

    public interface ITarget<TImageContainer, TImageView> : ITarget
    {
        TImageView Control { get; }

        void Set(IImageLoaderTask task, TImageContainer image, bool animated);

        void SetAsEmpty(IImageLoaderTask task);
    }
}

