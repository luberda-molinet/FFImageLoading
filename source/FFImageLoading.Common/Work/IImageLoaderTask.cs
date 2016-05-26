using System;
using System.Threading.Tasks;
using FFImageLoading.Cache;
using System.IO;

namespace FFImageLoading.Work
{
    public interface IImageLoaderTask: IScheduledWork, IDisposable
    {
		/// <summary>
		/// Indicates if the task uses the same native control
		/// </summary>
		/// <returns><c>true</c>, if same native control is used, <c>false</c> otherwise.</returns>
		/// <param name="task">Task to check.</param>
		bool UsesSameNativeControl(IImageLoaderTask task);

		/// <summary>
		/// Gets the cache key for this image loading task.
		/// </summary>
		/// <value>The cache key.</value>
        string GetKey(string path = null, bool raw = false);

		/// <summary>
		/// Indicates if memory cache should be used for the request
		/// </summary>
		/// <returns><c>true</c>, if memory cache should be used, <c>false</c> otherwise.</returns>
		/// <param name="path">Path.</param>
		bool CanUseMemoryCache(string path = null);

		/// <summary>
		/// Gets the parameters used to retrieve the image.
		/// </summary>
		/// <value>The parameters to retrieve the image.</value>
        TaskParameter Parameters { get; }

		/// <summary>
		/// Runs the image loading task: gets image from file, url, asset or cache. Then assign it to the imageView.
		/// </summary>
		Task RunAsync();

		/// <summary>
		/// Tries to load requested image from the cache asynchronously.
		/// </summary>
		/// <returns>A boolean indicating if image was loaded from cache.</returns>
		Task<CacheResult> TryLoadingFromCacheAsync();

		/// <summary>
		/// Prepares the instance before it runs.
		/// </summary>
		Task<bool> PrepareAndTryLoadingFromCacheAsync();

		/// <summary>
		/// Cancel current task only if needed
		/// </summary>
		void CancelIfNeeded();

		/// <summary>
		/// Loads the image from given stream asynchronously.
		/// </summary>
		/// <returns>An awaitable task.</returns>
		/// <param name="stream">The stream to get data from.</param>
		Task<GenerateResult> LoadFromStreamAsync(Stream stream);
    }
}

