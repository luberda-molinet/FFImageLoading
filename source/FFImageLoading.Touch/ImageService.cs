using System;
using System.IO;
using FFImageLoading.Cache;
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

        protected override IMemoryCache<UIImage> MemoryCache => ImageCache.Instance;
        protected override IMD5Helper CreatePlatformMD5HelperInstance() => new MD5Helper();
        protected override IMiniLogger CreatePlatformLoggerInstance() => new MiniLogger();
        protected override IPlatformPerformance CreatePlatformPerformanceInstance() => new PlatformPerformance();
        protected override IMainThreadDispatcher CreateMainThreadDispatcherInstance() => new MainThreadDispatcher();
        protected override IDataResolverFactory CreateDataResolverFactoryInstance() => new DataResolverFactory();

        protected override IDiskCache CreatePlatformDiskCacheInstance()
        {
            if (string.IsNullOrWhiteSpace(Config.DiskCachePath))
            {
                var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string tmpPath = Path.Combine(documents, "..", "Library", "Caches");
                string cachePath = Path.Combine(tmpPath, "FFSimpleDiskCache");
                Config.DiskCachePath = cachePath;
            }

            return new SimpleDiskCache(Config.DiskCachePath, Config);
        }

        internal static IImageLoaderTask CreateTask<TImageView>(TaskParameter parameters, ITarget<UIImage, TImageView> target) where TImageView : class
        {
            return new PlatformImageLoaderTask<TImageView>(target, parameters, Instance);
        }

        internal static IImageLoaderTask CreateTask(TaskParameter parameters)
        {
            return new PlatformImageLoaderTask<object>(null, parameters, Instance);
        }
    }
}
