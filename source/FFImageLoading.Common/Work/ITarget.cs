using System;
using System.Threading.Tasks;

namespace FFImageLoading.Work
{
	public interface ITarget<TBitmap, TImageLoaderTask>
		where TImageLoaderTask: IImageLoaderTask
	{
		bool IsValid { get; }

		bool IsTaskValid(TImageLoaderTask task);

		bool UsesSameNativeControl(TImageLoaderTask task);

		void Set(TImageLoaderTask task, TBitmap image, bool isLocalOrFromCache, bool isLoadingPlaceholder);

        void SetAsEmpty(TImageLoaderTask task);
	}
}

