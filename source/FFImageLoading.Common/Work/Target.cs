using System;

namespace FFImageLoading.Work
{
    public class Target<TImageContainer, TImageView>: ITarget<TImageContainer, TImageView>
	{
        public virtual TImageView Control { get { throw new NotImplementedException(); }}

		public virtual bool IsValid { get; } = true;

        public virtual bool IsTaskValid(IImageLoaderTask task) => IsValid;

		public virtual bool UsesSameNativeControl(IImageLoaderTask task) => false;

        public virtual void SetAsEmpty(IImageLoaderTask task) {  }

        public virtual void Set(IImageLoaderTask task, TImageContainer image, bool animated) { }

        public virtual void SetImageLoadingTask(IImageLoaderTask task) { }
    }
}

