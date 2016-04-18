using System;

namespace FFImageLoading.Work
{
	public interface IWorkScheduler
	{
		bool ExitTasksEarly { get; }

		/// <summary>
		/// Cancels any pending work for the task.
		/// </summary>
		/// <param name="task">Image loading task to cancel</param>
		void Cancel (IImageLoaderTask task);

		void SetExitTasksEarly (bool exitTasksEarly);

		void SetPauseWork (bool pauseWork);

		void RemovePendingTask (IImageLoaderTask task);

		/// <summary>
		/// Schedules the image loading. If image is found in cache then it returns it, otherwise it loads it.
		/// </summary>
		/// <param name="key">Key for cache lookup.</param>
		/// <param name="task">Image loading task.</param>
		void LoadImage (IImageLoaderTask task);
	}
}

