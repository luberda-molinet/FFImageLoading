using System;
using System.Threading.Tasks;

namespace FFImageLoading.Work
{
    public interface ITarget<TTargetImageContainer>
    {
        bool IsValid { get; }

        bool IsTaskValid(IImageLoaderTask task);

        bool UsesSameNativeControl(IImageLoaderTask task);

        void Set(IImageLoaderTask task, TTargetImageContainer image, bool animated);

        void SetAsEmpty(IImageLoaderTask task);
    }

	public interface ITarget<TBitmap, TImageLoaderTask> where TImageLoaderTask: IImageLoaderTask
	{
		bool IsValid { get; }

		bool IsTaskValid(TImageLoaderTask task);

		bool UsesSameNativeControl(TImageLoaderTask task);

		void Set(TImageLoaderTask task, TBitmap image, bool isLocalOrFromCache, bool isLoadingPlaceholder);

        void SetAsEmpty(TImageLoaderTask task);
	}
}

