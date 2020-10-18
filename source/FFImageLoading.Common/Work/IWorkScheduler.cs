using System;

namespace FFImageLoading.Work
{
    public interface IWorkScheduler
    {
        /// <summary>
        /// Cancels tasks that match predicate.
        /// </summary>
        /// <param name="predicate">Predicate for finding relevant tasks to cancel.</param>
        void Cancel(Func<IImageLoaderTask, bool> predicate);

		[Obsolete("Use SetPauseWork(bool pauseWork, bool cancelExistingTasks = false)")]
        bool ExitTasksEarly { get; }

		[Obsolete("Use SetPauseWork(bool pauseWork, bool cancelExistingTasks = false)")]
		void SetExitTasksEarly(bool exitTasksEarly);

        bool PauseWork { get; }

        void SetPauseWork(bool pauseWork, bool cancelExisting = false);

        void RemovePendingTask(IImageLoaderTask task);

        /// <summary>
        /// Schedules the image loading. If image is found in cache then it returns it, otherwise it loads it.
        /// </summary>
        /// <param name="task">Image loading task.</param>
        void LoadImage(IImageLoaderTask task);
    }
}
