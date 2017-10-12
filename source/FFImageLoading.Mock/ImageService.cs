using System;
using FFImageLoading.Mock;
using FFImageLoading.Helpers;
using FFImageLoading.Cache;
using System.Reflection;
using System.Linq;
using FFImageLoading.Work;

namespace FFImageLoading
{
    /// <summary>
    /// FFImageLoading by Daniel Luberda
    /// </summary>
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

        static bool EnableMockImageService
        {
            get
            {
                try
                {
                    var assemblyMethod = typeof(Assembly).GetRuntimeMethods()
                                .Where(m => m.Name.Equals("GetExecutingAssembly"))
                                .FirstOrDefault();
                    var assembly = assemblyMethod?.Invoke(null, null) as Assembly;
                    if (assembly != null)
                    {
                        var attributes = assembly.GetCustomAttributes(typeof(MockFFImageLoadingAttribute));
                        if (attributes != null && attributes.Count() > 0)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                }

                return false;
            }
        }

        protected override IMemoryCache<MockBitmap> MemoryCache => MockImageCache.Instance;
        protected override IMD5Helper CreatePlatformMD5HelperInstance() => new MockMD5Helper();
        protected override IMiniLogger CreatePlatformLoggerInstance() => new MockLogger();
        protected override IDiskCache CreatePlatformDiskCacheInstance() => new MockDiskCache();
        protected override IPlatformPerformance CreatePlatformPerformanceInstance() => new EmptyPlatformPerformance();
        protected override IDataResolverFactory CreateDataResolverFactoryInstance() => new MockDataResolverFactory();
        protected override IMainThreadDispatcher CreateMainThreadDispatcherInstance() => new MockMainThreadDispatcher();

        internal static IImageLoaderTask CreateTask<TImageView>(TaskParameter parameters, ITarget<MockBitmap, TImageView> target) where TImageView : class
        {
            return new PlatformImageLoaderTask<TImageView>(target, parameters, Instance);
        }

        internal static IImageLoaderTask CreateTask(TaskParameter parameters)
        {
            return new PlatformImageLoaderTask<object>(null, parameters, Instance);
        }
    }
}

