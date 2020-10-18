using System.Collections.Generic;
using System;
using FFImageLoading.Config;
using FFImageLoading.Work;
using System.Net.Http;
using FFImageLoading.Helpers;
using FFImageLoading.Cache;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;

namespace FFImageLoading
{
    /// <summary>
    /// FFImageLoading by Daniel Luberda
    /// </summary>
    [Preserve(AllMembers = true)]
    public interface IImageService
    {
        /// <summary>
        /// Gets FFImageLoading configuration
        /// </summary>
        /// <value>The configuration used by FFImageLoading.</value>
        Configuration Config { get; }

        /// <summary>
        /// Initializes FFImageLoading with a default Configuration.
        /// Also forces to run disk cache cleaning routines (avoiding delay for first image loading tasks)
        /// </summary>
        void Initialize();

        /// <summary>
        /// Initializes FFImageLoading with a given Configuration. It allows to configure and override most of it.
        /// Also forces to run disk cache cleaning routines (avoiding delay for first image loading tasks)
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        void Initialize(Configuration configuration);

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a file.
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="filepath">Path to the file.</param>
        TaskParameter LoadFile(string filepath);

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a URL.
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="url">URL to the file</param>
        /// <param name="cacheDuration">How long the file will be cached on disk</param>
        TaskParameter LoadUrl(string url, TimeSpan? cacheDuration = null);

        /// <summary>
        /// Loads the string.
        /// </summary>
        /// <returns>The string.</returns>
        /// <param name="data">Data.</param>
        /// <param name="encoding">Encoding.</param>
        TaskParameter LoadString(string data, DataEncodingType encoding = DataEncodingType.RAW);

        /// <summary>
        /// Loads the base64 string.
        /// </summary>
        /// <returns>The base64 string.</returns>
        /// <param name="data">Data.</param>
        TaskParameter LoadBase64String(string data);

		/// <summary>
		/// Load an image from a file from application bundle.
		/// eg. assets on Android, compiled resource for other platforms
		/// </summary>
		/// <returns>The new TaskParameter.</returns>
		/// <param name="filepath">Path to the file.</param>
		TaskParameter LoadFileFromApplicationBundle(string filepath);

		/// <summary>
		/// Load an image from a file from application resource.
		/// </summary>
		/// <returns>The new TaskParameter.</returns>
		/// <param name="resourceName">Name of the resource in drawable folder without extension</param>
		TaskParameter LoadCompiledResource(string resourceName);

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a compiled drawable resource.
        /// eg. resource://YourProject.Resource.Resource.png
        /// eg. resource://YourProject.Resource.Resource.png?assembly=[FULL_ASSEMBLY_NAME]
        /// </summary>
        TaskParameter LoadEmbeddedResource(string resourceUri);

		/// <summary>
		/// Constructs a new TaskParameter to load an image from a compiled drawable resource.
		/// eg. resource://YourProject.Resource.Resource.png
		/// eg. resource://YourProject.Resource.Resource.png?assembly=[FULL_ASSEMBLY_NAME]
		/// </summary>
		/// <returns>The new TaskParameter.</returns>
		TaskParameter LoadEmbeddedResource(string resourceName, Assembly resourceAssembly);

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a Stream.
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="stream">Stream.</param>
        TaskParameter LoadStream(Func<CancellationToken, Task<Stream>> stream);

        /// <summary>
        /// Gets a value indicating whether ImageService will exit tasks earlier
        /// </summary>
        /// <value><c>true</c> if it should exit tasks early; otherwise, <c>false</c>.</value>
        bool ExitTasksEarly { get; }

        /// <summary>
        /// Sets a value indicating whether ImageService will exit tasks earlier
        /// </summary>
        /// <param name="exitTasksEarly">If set to <c>true</c> exit tasks early.</param>
        void SetExitTasksEarly(bool exitTasksEarly);

        /// <summary>
        /// Gets a value indicating whether ImageService will pause tasks execution
        /// </summary>
        /// <value><c>true</c> if pause work; otherwise, <c>false</c>.</value>
        bool PauseWork { get; }

		/// <summary>
		/// Sets a value indicating if all loading work should be paused.
		/// </summary>
		/// <param name="pauseWork">If set to <c>true</c> pause work.</param>
		/// <param name="cancelExisting">If set to <c>true</c> cancels existing tasks.</param>
		void SetPauseWork(bool pauseWork, bool cancelExisting = false);

		/// <summary>
		/// Sets a value indicating if all loading work should be paused. Also cancels existing tasks.
		/// </summary>
		/// <param name="pauseWork">If set to <c>true</c> pause work.</param>
		void SetPauseWorkAndCancelExisting(bool pauseWork);

		/// <summary>
		/// Cancel any loading work for the given task
		/// </summary>
		/// <param name="task">Image loading task to cancel.</param>
		void CancelWorkFor(IImageLoaderTask task);

        /// <summary>
        /// Cancel any loading work for the given view
        /// </summary>
        /// <param name="view">Image loading task to cancel.</param>
        void CancelWorkForView(object view);

        /// <summary>
        /// Removes a pending image loading task from the work queue.
        /// </summary>
        /// <param name="task">Image loading task to remove.</param>
        void RemovePendingTask(IImageLoaderTask task);

        /// <summary>
        /// Queue an image loading task.
        /// </summary>
        /// <param name="task">Image loading task.</param>
        void LoadImage(IImageLoaderTask task);

        /// <summary>
        /// Invalidates selected caches.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        /// <param name="cacheType">Memory cache, Disk cache or both</param>
        Task InvalidateCacheAsync(CacheType cacheType);

        /// <summary>
        /// Invalidates the memory cache.
        /// </summary>
        void InvalidateMemoryCache();

        /// <summary>
        /// Invalidates the disk cache.
        /// </summary>
        Task InvalidateDiskCacheAsync();

        /// <summary>
        /// Invalidates the cache for given key.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="key">Concerns images with this key.</param>
        /// <param name="cacheType">Memory cache, Disk cache or both</param>
        /// <param name="removeSimilar">If similar keys should be removed, ie: typically keys with extra transformations</param>
        Task InvalidateCacheEntryAsync(string key, CacheType cacheType, bool removeSimilar=false);

        /// <summary>
        /// Cancels tasks that match predicate.
        /// </summary>
        /// <param name="predicate">Predicate for finding relevant tasks to cancel.</param>
        void Cancel(Func<IImageLoaderTask, bool> predicate);

        /// <summary>
        /// Cancels tasks that match predicate.
        /// </summary>
        /// <param name="predicate">Predicate for finding relevant tasks to cancel.</param>
        void Cancel(Func<TaskParameter, bool> predicate);

        /// <summary>
        /// Dps to pixels.
        /// </summary>
        /// <returns>The to pixels.</returns>
        /// <param name="dp">Dp.</param>
        int DpToPixels(double dp);

        /// <summary>
        /// Pixelses to dp.
        /// </summary>
        /// <returns>The to dp.</returns>
        /// <param name="px">Px.</param>
        double PixelsToDp(double px);
    }
}
