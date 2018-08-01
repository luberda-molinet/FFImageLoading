using System;
using FFImageLoading.Mock;
using FFImageLoading.Helpers;
using FFImageLoading.Cache;
using System.Reflection;
using System.Linq;
using FFImageLoading.Work;
using FFImageLoading.Config;

namespace FFImageLoading
{
    /// <summary>
    /// FFImageLoading by Daniel Luberda
    /// </summary>
    [Preserve(AllMembers = true)]
    public class ImageService : ImageServiceBase<MockBitmap>
    {
        const string DoNotReference = "You are referencing the portable assembly - you need to reference the platform specific assembly";

        static IImageService _instance;

        /// <summary>
        /// FFImageLoading instance.
        /// </summary>
        /// <value>The instance.</value>
        public static IImageService Instance
        {
            get
            {
                if (!EnableMockImageService)
                    throw new NotImplementedException(DoNotReference);

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

        protected override IMemoryCache<MockBitmap> MemoryCache => MockImageCache.Instance;
        protected override IMD5Helper CreatePlatformMD5HelperInstance(Configuration configuration) => new MockMD5Helper();
        protected override IMiniLogger CreatePlatformLoggerInstance(Configuration configuration) => new MockLogger();
        protected override IDiskCache CreatePlatformDiskCacheInstance(Configuration configuration) => new MockDiskCache();
        protected override IPlatformPerformance CreatePlatformPerformanceInstance(Configuration configuration) => new EmptyPlatformPerformance();
        protected override IDataResolverFactory CreateDataResolverFactoryInstance(Configuration configuration) => new MockDataResolverFactory();
        protected override IMainThreadDispatcher CreateMainThreadDispatcherInstance(Configuration configuration) => new MockMainThreadDispatcher();

        internal static IImageLoaderTask CreateTask<TImageView>(TaskParameter parameters, ITarget<MockBitmap, TImageView> target) where TImageView : class
        {
            return new PlatformImageLoaderTask<TImageView>(target, parameters, Instance);
        }

        internal static IImageLoaderTask CreateTask(TaskParameter parameters)
        {
            return new PlatformImageLoaderTask<object>(null, parameters, Instance);
        }

        protected override void SetTaskForTarget(IImageLoaderTask currentTask)
        {
            // throw new NotImplementedException();
        }

        public override void CancelWorkForView(object view)
        {
            // throw new NotImplementedException();
        }

        public override int DpToPixels(double dp)
        {
            return (int)dp;
        }

        public override double PixelsToDp(double px)
        {
            return px;
        }
    }
}

