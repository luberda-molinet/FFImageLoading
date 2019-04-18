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
        static IImageService _instance;

        /// <summary>
        /// FFImageLoading instance.
        /// </summary>
        /// <value>The instance.</value>
        public static IImageService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ImageService();
                
                return _instance;
            }
        }

        /// <summary>
        /// Set this to use FFImageLoading in a unit test environment. 
        /// Instead throwing DoNotReference exception - use Mock implementation
        /// </summary>
        public static bool EnableMockImageService { get; set; }

        protected override IMemoryCache<UIImage> MemoryCache => ImageCache.Instance;
        protected override IMD5Helper CreatePlatformMD5HelperInstance(Configuration configuration) => new MD5Helper();
        protected override IMiniLogger CreatePlatformLoggerInstance(Configuration configuration) => new MiniLogger();
        protected override IPlatformPerformance CreatePlatformPerformanceInstance(Configuration configuration) => new PlatformPerformance();
        protected override IMainThreadDispatcher CreateMainThreadDispatcherInstance(Configuration configuration) => new MainThreadDispatcher();
        protected override IDataResolverFactory CreateDataResolverFactoryInstance(Configuration configuration) => new DataResolverFactory();

        protected override IDiskCache CreatePlatformDiskCacheInstance(Configuration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.DiskCachePath))
            {
                var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string tmpPath = Path.Combine(documents, "..", "Library", "Caches");
                string cachePath = Path.Combine(tmpPath, "FFSimpleDiskCache");
                configuration.DiskCachePath = cachePath;
            }

            return new SimpleDiskCache(configuration.DiskCachePath, configuration);
        }

        internal static IImageLoaderTask CreateTask<TImageView>(TaskParameter parameters, ITarget<UIImage, TImageView> target) where TImageView : class
        {
            return new PlatformImageLoaderTask<TImageView>(target, parameters, Instance);
        }

        internal static IImageLoaderTask CreateTask(TaskParameter parameters)
        {
            return new PlatformImageLoaderTask<object>(null, parameters, Instance);
        }

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

        public override int DpToPixels(double dp)
        {
            return (int)Math.Floor(dp * ScaleHelper.Scale);
        }

        public override double PixelsToDp(double px)
        {
            if (Math.Abs(px) < double.Epsilon)
                return 0d;

            return px / ScaleHelper.Scale;
        }
    }
}
