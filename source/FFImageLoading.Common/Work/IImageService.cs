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
    [Preserve(AllMembers = true)]
    public interface IImageService
    {
		/// <summary>
		/// Gets FFImageLoading configuration
		/// </summary>
		/// <value>The configuration used by FFImageLoading.</value>
		Configuration Config { get; }

		/// Initializes FFImageLoading with a default Configuration. 
        /// Also forces to run disk cache cleaning routines (avoiding delay for first image loading tasks)
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        void Initialize();
        
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
		/// Constructs a new TaskParameter to load an image from a file from application bundle.
		/// </summary>
		/// <returns>The new TaskParameter.</returns>
		/// <param name="filepath">Path to the file.</param>
		TaskParameter LoadFileFromApplicationBundle(string filepath);

		/// <summary>
		/// Constructs a new TaskParameter to load an image from a compiled drawable resource.
		/// </summary>
		/// <returns>The new TaskParameter.</returns>
		/// <param name="resourceName">Name of the resource in drawable folder without extension</param>
		TaskParameter LoadCompiledResource(string resourceName);

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a compiled drawable resource.
        /// eg. resource://YourProject.Resource.Resource.png
        /// eg. resource://YourProject.Resource.Resource.png?assembly=[FULL_ASSEMBLY_NAME]
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="resourceUri">Uri of the resource</param>
        TaskParameter LoadEmbeddedResource(string resourceUri);

        TaskParameter LoadEmbeddedResource(string resourceName, Assembly resourceAssembly);

		/// <summary>
		/// Constructs a new TaskParameter to load an image from a Stream.
		/// </summary>
		/// <returns>The new TaskParameter.</returns>
		/// <param name="resourceName">A function that allows a CancellationToken and returns the Stream to use. This function will be invoked by LoadStream().</param>
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
        /// Sets a value indicating if all loading work should be paused (silently canceled).
        /// </summary>
        /// <param name="pauseWork">If set to <c>true</c> pause/cancel work.</param>
		void SetPauseWork(bool pauseWork);

        /// <summary>
        /// Cancel any loading work for the given ImageView
        /// </summary>
        /// <param name="task">Image loading task to cancel.</param>
		void CancelWorkFor(IImageLoaderTask task);

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
	}
}
