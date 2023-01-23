using System;
using System.Threading.Tasks;

namespace FFImageLoading.Work
{
    public interface ITarget
    {
        bool IsValid { get; }

        object TargetControl { get; }
    }

    public interface ITarget<TImageContainer, TImageView> : ITarget
    {
        TImageView Control { get; }

        void Set(IImageLoaderTask task, TImageContainer image, bool animated);

        void SetAsEmpty(IImageLoaderTask task);
    }
}

