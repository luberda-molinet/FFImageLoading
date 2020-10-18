namespace FFImageLoading.Work
{
    public class Target<TImageContainer, TImageView>: ITarget<TImageContainer, TImageView>
    {
        public virtual TImageView Control => default;

        public virtual bool IsValid { get; } = true;

        public object TargetControl => Control;

        public virtual void SetAsEmpty(IImageLoaderTask task) {  }

        public virtual void Set(IImageLoaderTask task, TImageContainer image, bool animated) { }
    }
}

