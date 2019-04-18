using System;
using System.IO;
using FFImageLoading.Cache;
using FFImageLoading.Drawables;
using FFImageLoading.Helpers;
using FFImageLoading.Work;
using FFImageLoading.DataResolvers;
using System.Runtime.CompilerServices;
using FFImageLoading.Config;

namespace FFImageLoading
{
    /// <summary>
    /// FFImageLoading by Daniel Luberda
    /// </summary>
    [Preserve(AllMembers = true)]
    public class ImageService : ImageServiceBase<SelfDisposingBitmapDrawable>
    {
        readonly Android.Util.DisplayMetrics _metrics = Android.Content.Res.Resources.System.DisplayMetrics;

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

        protected override IMemoryCache<SelfDisposingBitmapDrawable> MemoryCache => ImageCache.Instance;
        protected override IMD5Helper CreatePlatformMD5HelperInstance(Configuration configuration) => new MD5Helper();
        protected override IMiniLogger CreatePlatformLoggerInstance(Configuration configuration) => new MiniLogger();
        protected override IPlatformPerformance CreatePlatformPerformanceInstance(Configuration configuration) => new PlatformPerformance();
        protected override IMainThreadDispatcher CreateMainThreadDispatcherInstance(Configuration configuration) => new MainThreadDispatcher();
        protected override IDataResolverFactory CreateDataResolverFactoryInstance(Configuration configuration) => new DataResolverFactory();

        protected override IDiskCache CreatePlatformDiskCacheInstance(Configuration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.DiskCachePath))
            {
                var context = new Android.Content.ContextWrapper(Android.App.Application.Context);
                string tmpPath = context.CacheDir.AbsolutePath;
                string cachePath = Path.Combine(tmpPath, "FFSimpleDiskCache");

                Java.IO.File androidTempFolder = new Java.IO.File(cachePath);
                if (!androidTempFolder.Exists())
                    androidTempFolder.Mkdir();

                if (!androidTempFolder.CanRead())
                    androidTempFolder.SetReadable(true, false);

                if (!androidTempFolder.CanWrite())
                    androidTempFolder.SetWritable(true, false);

                configuration.DiskCachePath = cachePath;
            }

            return new SimpleDiskCache(configuration.DiskCachePath, configuration);
        }

        internal static IImageLoaderTask CreateTask<TImageView>(TaskParameter parameters, ITarget<SelfDisposingBitmapDrawable, TImageView> target) where TImageView : class
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

            if (!(targetView is Android.Views.View))
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
            double px = dp * ((float)_metrics.DensityDpi / 160f);
            return (int)Math.Floor(px);
        }

        public override double PixelsToDp(double px)
        {
            if (Math.Abs(px) < double.Epsilon)
                return 0;

            return px / ((float)_metrics.DensityDpi / 160f);
        }
    }
}
