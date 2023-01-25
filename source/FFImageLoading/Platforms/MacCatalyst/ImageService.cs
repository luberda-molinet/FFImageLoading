using System;
using System.IO;
using System.Runtime.CompilerServices;
using FFImageLoading.Cache;
using FFImageLoading.Config;
using FFImageLoading.DataResolvers;
using FFImageLoading.Helpers;
using FFImageLoading.Work;
using UIKit;

namespace FFImageLoading
{
    /// <summary>
    /// FFImageLoading by Daniel Luberda
    /// </summary>
    [Preserve(AllMembers = true)]
    public class ImageService : ImageServiceBase<UIImage>
    {
        static ConditionalWeakTable<object, IImageLoaderTask> _viewsReferences = new ConditionalWeakTable<object, IImageLoaderTask>();

		public ImageService(IConfiguration configuration, IMD5Helper md5Helper, IMiniLogger miniLogger, IPlatformPerformance platformPerformance, IMainThreadDispatcher mainThreadDispatcher, IDataResolverFactory dataResolverFactory, IDownloadCache downloadCache,
			IWorkScheduler workScheduler) : base(configuration, md5Helper, miniLogger, platformPerformance, mainThreadDispatcher, dataResolverFactory, downloadCache, workScheduler)
		{
		}

		ImageCache imageCache;

		public override IMemoryCache<UIImage> MemoryCache => imageCache ??= new ImageCache(Configuration, Logger);


        public override IImageLoaderTask CreateTask<TImageView>(TaskParameter parameters, ITarget<UIImage, TImageView> target) where TImageView : class
			=> new PlatformImageLoaderTask<TImageView>(this, target, parameters);

		public override IImageLoaderTask CreateTask(TaskParameter parameters)
			=> new PlatformImageLoaderTask<object>(this, null, parameters);

        protected override void SetTaskForTarget(IImageLoaderTask currentTask)
        {
            var targetView = currentTask?.Target?.TargetControl;

            if (!(targetView is UIView))
                return;

            lock (_viewsReferences)
            {
                if (_viewsReferences.TryGetValue(targetView, out var existingTask))
                {
                    try
                    {
                        if (existingTask != null && !existingTask.IsCancelled && !existingTask.IsCompleted)
                        {
                            existingTask.Cancel();
                        }
                    }
                    catch (ObjectDisposedException) { }

                    _viewsReferences.Remove(targetView);
                }

                _viewsReferences.Add(targetView, currentTask);
            }
        }

        public override void CancelWorkForView(object view)
        {
            lock (_viewsReferences)
            {
                if (_viewsReferences.TryGetValue(view, out var existingTask))
                {
                    try
                    {
                        if (existingTask != null && !existingTask.IsCancelled && !existingTask.IsCompleted)
                        {
                            existingTask.Cancel();
                        }
                    }
                    catch (ObjectDisposedException) { }
                }
            }
        }

        public override int DpToPixels(double dp, double scale)
        {
            return (int)Math.Floor(dp * scale);
        }

        public override double PixelsToDp(double px, double scale)
        {
            if (Math.Abs(px) < double.Epsilon)
                return 0d;

            return px / scale;
        }
    }
}
