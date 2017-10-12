using System;
using System.IO;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using FFImageLoading.Work;
using Windows.UI.Xaml.Media.Imaging;

namespace FFImageLoading
{
    /// <summary>
    /// FFImageLoading by Daniel Luberda
    /// </summary>
    public class ImageService : ImageServiceBase<WriteableBitmap>
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

        protected override void PlatformSpecificConfiguration(Config.Configuration configuration)
        {
            base.OnConfigurationChanged(configuration);

            configuration.ClearMemoryCacheOnOutOfMemory = false;
            configuration.ExecuteCallbacksOnUIThread = true;
        }

        protected override IMemoryCache<WriteableBitmap> MemoryCache => ImageCache.Instance;
        protected override IMD5Helper CreatePlatformMD5HelperInstance() => new MD5Helper();
        protected override IMiniLogger CreatePlatformLoggerInstance() => new MiniLogger();
        protected override IPlatformPerformance CreatePlatformPerformanceInstance() => new PlatformPerformance();
        protected override IMainThreadDispatcher CreateMainThreadDispatcherInstance() => new MainThreadDispatcher();
        protected override IDataResolverFactory CreateDataResolverFactoryInstance() => new DataResolverFactory();

        protected override IDiskCache CreatePlatformDiskCacheInstance()
        {
            StorageFolder rootFolder = null;
            string folderName = null;

            if (string.IsNullOrWhiteSpace(Config.DiskCachePath))
            {
                rootFolder = ApplicationData.Current.TemporaryFolder;
                folderName = "FFSimpleDiskCache";
                string cachePath = Path.Combine(rootFolder.Path, folderName);
                Config.DiskCachePath = cachePath;
            }
            else
            {
                var rootPath = Directory.GetParent(path).FullName;
                folderName = new DirectoryInfo(path).Name;
                rootFolder = StorageFolder.GetFolderFromPathAsync(Config.DiskCachePath)
                                          .ConfigureAwait(false).GetAwaiter().GetResult();
            }

            return new SimpleDiskCache(rootFolder, folderName, Config);
        }

        internal static IImageLoaderTask CreateTask<TImageView>(TaskParameter parameters, ITarget<WriteableBitmap, TImageView> target) where TImageView : class
        {
            return new PlatformImageLoaderTask<TImageView>(target, parameters, Instance);
        }

        internal static IImageLoaderTask CreateTask(TaskParameter parameters)
        {
            return new PlatformImageLoaderTask<object>(null, parameters, Instance);
        }
    }
}
