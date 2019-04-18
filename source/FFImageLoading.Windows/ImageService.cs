using System;
using System.IO;
using FFImageLoading.Config;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using FFImageLoading.Work;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using FFImageLoading.DataResolvers;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FFImageLoading
{
    /// <summary>
    /// FFImageLoading by Daniel Luberda
    /// </summary>
    [Preserve(AllMembers = true)]
    public class ImageService : ImageServiceBase<BitmapSource>
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

        protected override void PlatformSpecificConfiguration(Config.Configuration configuration)
        {
            base.PlatformSpecificConfiguration(configuration);

            configuration.ClearMemoryCacheOnOutOfMemory = false;
            configuration.ExecuteCallbacksOnUIThread = true;
        }

        protected override IMemoryCache<BitmapSource> MemoryCache => ImageCache.Instance;
        protected override IMD5Helper CreatePlatformMD5HelperInstance(Configuration configuration) => new MD5Helper();
        protected override IMiniLogger CreatePlatformLoggerInstance(Configuration configuration) => new MiniLogger();
        protected override IPlatformPerformance CreatePlatformPerformanceInstance(Configuration configuration) => new PlatformPerformance();
        protected override IMainThreadDispatcher CreateMainThreadDispatcherInstance(Configuration configuration) => new MainThreadDispatcher();
        protected override IDataResolverFactory CreateDataResolverFactoryInstance(Configuration configuration) => new DataResolverFactory();

        protected override IDiskCache CreatePlatformDiskCacheInstance(Configuration configuration)
        {
            StorageFolder rootFolder = null;
            string folderName = null;

            if (string.IsNullOrWhiteSpace(configuration.DiskCachePath))
            {
                rootFolder = ApplicationData.Current.TemporaryFolder;
                folderName = "FFSimpleDiskCache";
                string cachePath = Path.Combine(rootFolder.Path, folderName);
                configuration.DiskCachePath = cachePath;
            }
            else
            {
                var separated = configuration.DiskCachePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                folderName = separated.Last();
                var rootPath = configuration.DiskCachePath.Substring(0, configuration.DiskCachePath.LastIndexOf(folderName));
                rootFolder = StorageFolder.GetFolderFromPathAsync(rootPath).GetAwaiter().GetResult();
            }

            return new SimpleDiskCache(rootFolder, folderName, Config);
        }

        internal static IImageLoaderTask CreateTask<TImageView>(TaskParameter parameters, ITarget<BitmapSource, TImageView> target) where TImageView : class
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

            if (!(targetView is Windows.UI.Xaml.Controls.Image))
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

            return Math.Floor(px / ScaleHelper.Scale);
        }
    }
}
