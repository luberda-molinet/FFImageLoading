using System;
using System.Threading.Tasks;

namespace FFImageLoading.Work
{
    public interface IImageLoaderTask: IScheduledWork
    {
		/// <summary>
		/// Gets the cache key for this image loading task.
		/// </summary>
		/// <value>The cache key.</value>
        string Key { get; }

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
		Task<bool> TryLoadingFromCacheAsync();

		/// <summary>
		/// Prepares the instance before it runs.
		/// </summary>
        Task PrepareAsync();
    }
}

