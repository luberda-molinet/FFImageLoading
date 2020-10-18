using System;
using System.IO;
using System.Runtime.CompilerServices;
using FFImageLoading.Cache;
using FFImageLoading.Config;
using FFImageLoading.DataResolvers;
using FFImageLoading.Helpers;
using FFImageLoading.Views;
using FFImageLoading.Work;
using ElmSharp;
using AppFW = Tizen.Applications;
using TSystemInfo = Tizen.System.Information;

namespace FFImageLoading
{
    /// <summary>
    /// FFImageLoading for Tizen
    /// </summary>
    [Preserve(AllMembers = true)]
    public class ImageService : ImageServiceBase<SharedEvasImage>
    {
        static ConditionalWeakTable<object, IImageLoaderTask> s_viewsReferences = new ConditionalWeakTable<object, IImageLoaderTask>();
        static Lazy<IImageService> _instance = new Lazy<IImageService>(() =>
        {
            return new ImageService();
        });

        static Lazy<int> s_dpi = new Lazy<int>(() =>
        {
            int dpi = 0;
            if (Elementary.GetProfile() == "tv")
                return 72;

            TSystemInfo.TryGetValue<int>("http://tizen.org/feature/screen.dpi", out dpi);
            return dpi;
        });

        /// <summary>
        /// FFImageLoading instance.
        /// </summary>
        /// <value>The instance.</value>
        public static IImageService Instance => _instance.Value;

        /// <summary>
        /// Set this to use FFImageLoading in a unit test environment. 
        /// Instead throwing DoNotReference exception - use Mock implementation
        /// </summary>
        public static bool EnableMockImageService { get; set; }

        public static Func<EvasObject> MainWindowProvider { get; set; }

        protected override IMemoryCache<SharedEvasImage> MemoryCache => EvasImageCache.Instance;
        protected override IMD5Helper CreatePlatformMD5HelperInstance(Configuration configuration) => new MD5Helper();
        protected override IMiniLogger CreatePlatformLoggerInstance(Configuration configuration) => new MiniLogger();
        protected override IPlatformPerformance CreatePlatformPerformanceInstance(Configuration configuration) => new EmptyPlatformPerformance();
        protected override IMainThreadDispatcher CreateMainThreadDispatcherInstance(Configuration configuration) => new EcoreThreadDispatcher();
        protected override IDataResolverFactory CreateDataResolverFactoryInstance(Configuration configuration) => new DataResolverFactory();

        protected override IDiskCache CreatePlatformDiskCacheInstance(Configuration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.DiskCachePath))
            {
                var appCachePath = AppFW.Application.Current.DirectoryInfo.Cache;
                string cachePath = Path.Combine(appCachePath, "FFSimpleDiskCache");
                configuration.DiskCachePath = cachePath;
            }
            return new SimpleDiskCache(configuration.DiskCachePath, configuration);
        }

        protected override void SetTaskForTarget(IImageLoaderTask currentTask)
        {
            var targetView = currentTask?.Target?.TargetControl;

            if (!(targetView is EvasImageContainer))
                return;

            lock (s_viewsReferences)
            {
                CancelWorkForView(targetView);
                s_viewsReferences.Add(targetView, currentTask);
            }
        }

        public override void CancelWorkForView(object view)
        {
            lock (s_viewsReferences)
            {
                if (s_viewsReferences.TryGetValue(view, out var existingTask))
                {
                    try
                    {
                        if (existingTask != null && !existingTask.IsCancelled && !existingTask.IsCompleted)
                        {
                            existingTask.Cancel();
                        }
                    }
                    catch (ObjectDisposedException) { }
                    s_viewsReferences.Remove(view);
                }
            }
        }

        internal static IImageLoaderTask CreateTask<TImageView>(TaskParameter parameters, ITarget<SharedEvasImage, TImageView> target) where TImageView : class
        {
            return new PlatformImageLoaderTask<TImageView>(target, parameters, Instance);
        }

        internal static IImageLoaderTask CreateTask(TaskParameter parameters)
        {
            return new PlatformImageLoaderTask<object>(null, parameters, Instance);
        }

        public override int DpToPixels(double dp)
        {
            return (int)Math.Round(dp * s_dpi.Value / 160.0);
        }

        public override double PixelsToDp(double px)
        {
            return px / (s_dpi.Value / 160.0);
        }
    }
}
