using System;
using System.Threading.Tasks;

namespace FFImageLoading.Work
{
	public class Target<TBitmap, TImageLoaderTask>: ITarget<TBitmap, TImageLoaderTask>
		where TImageLoaderTask: IImageLoaderTask
	{
		public virtual bool IsValid { get; } = true;

		public virtual bool IsTaskValid(TImageLoaderTask task) => IsValid;

		public virtual void Set(TImageLoaderTask task, TBitmap image, bool isLocalOrFromCache, bool isLoadingPlaceholder) { }

		public virtual bool UsesSameNativeControl(TImageLoaderTask task) => false;

        public virtual void SetAsEmpty() { }
	}
}

