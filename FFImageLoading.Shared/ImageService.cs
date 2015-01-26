using Android.Widget;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using System;
using FFImageLoading.Config;
using FFImageLoading.Work;
using System.Net.Http;
using FFImageLoading.Helpers;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using FFImageLoading.Cache;
using FFImageLoading.Extensions;

namespace FFImageLoading
{
    public static class ImageService
    {
        private static bool _initialized;

        public static Configuration Config { get; private set; }

        public static void Initialize(HttpClient httpClient = null, IWorkScheduler scheduler = null, IMiniLogger logger = null,
            IDiskCache diskCache = null, IDownloadCache downloadCache = null)
        {
            if (_initialized) {
                System.Diagnostics.Debug.WriteLine("FFImageLoading.ImageService is already initialized, nothing will happen");
                return;
            }

            var userDefinedConfig = new Configuration(httpClient, scheduler, logger, diskCache, downloadCache);
            Config = GetDefaultConfiguration(userDefinedConfig);

            _initialized = true;
        }

        private static Configuration GetDefaultConfiguration(Configuration userDefinedConfig)
        {
            var context = Android.App.Application.Context.ApplicationContext;
            var httpClient = userDefinedConfig.HttpClient ?? new HttpClient();
            var logger = userDefinedConfig.Logger ?? new MiniLogger();
            var scheduler = userDefinedConfig.Scheduler ?? new WorkScheduler(logger);
            var diskCache = userDefinedConfig.DiskCache ?? DiskCache.CreateCache(context, typeof(ImageService).Name);
            var downloadCache = userDefinedConfig.DownloadCache ?? new DownloadCache(httpClient, diskCache);

            return new Configuration(
                httpClient,
                scheduler,
                logger,
                diskCache,
                downloadCache
            );
        }

        private static IWorkScheduler Scheduler
        {
            get {
                Initialize();
                return Config.Scheduler;
            }
        }

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a file.
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="filepath">Path to the file.</param>
        public static TaskParameter LoadFile(string filepath)
        {
            Initialize();
            return TaskParameter.FromFile(filepath);
        }

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a URL.
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="url">URL to the file</param>
        /// <param name="cacheDuration">How long the file will be cached on disk</param>
        public static TaskParameter LoadUrl(string url, TimeSpan? cacheDuration = null)
        {
            Initialize();
            return TaskParameter.FromUrl(url, cacheDuration);
        }

        /// <summary>
        /// Gets a value indicating whether ImageService will exit tasks earlier
        /// </summary>
        /// <value><c>true</c> if it should exit tasks early; otherwise, <c>false</c>.</value>
        public static bool ExitTasksEarly
        {
            get
            {
                return Scheduler.ExitTasksEarly;
            }
        }

        /// <summary>
        /// Sets a value indicating whether ImageService will exit tasks earlier
        /// </summary>
        /// <param name="exitTasksEarly">If set to <c>true</c> exit tasks early.</param>
        public static void SetExitTasksEarly(bool exitTasksEarly)
        {
            Scheduler.SetExitTasksEarly(exitTasksEarly);
        }

        /// <summary>
        /// Sets a value indicating if all loading work should be paused (silently canceled).
        /// </summary>
        /// <param name="pauseWork">If set to <c>true</c> pause/cancel work.</param>
        public static void SetPauseWork(bool pauseWork)
        {
            Scheduler.SetPauseWork(pauseWork);
        }

        /// <summary>
        /// Cancel any loading work for the given ImageView
        /// </summary>
        /// <param name="imageView">Image view.</param>
        public static void CancelWorkFor(ImageView imageView)
        {
            Scheduler.Cancel(imageView.GetImageLoaderTask());
        }

        /// <summary>
        /// Removes a pending image loading task from the work queue.
        /// </summary>
        /// <param name="task">Image loading task to remove.</param>
        public static void RemovePendingTask(IImageLoaderTask task)
        {
            Scheduler.RemovePendingTask(task);
        }

        /// <summary>
        /// Queue an image loading task.
        /// </summary>
        /// <param name="key">Key used for caching.</param>
        /// <param name="task">Image loading task.</param>
        /// <param name="imageView">Image view that will receive the loaded image.</param>
        public static void LoadImage(IImageLoaderTask task)
        {
            Scheduler.LoadImage(task);
        }
    }
}