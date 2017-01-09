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

        bool ExitTasksEarly { get; }

        void SetExitTasksEarly(bool exitTasksEarly);

        bool PauseWork { get; }

        void SetPauseWork(bool pauseWork);

        void RemovePendingTask(IImageLoaderTask task);

        /// <summary>      
        /// Schedules the image loading. If image is found in cache then it returns it, otherwise it loads it.        
        /// </summary>        
        /// <param name="key">Key for cache lookup.</param>       
        /// <param name="task">Image loading task.</param>
        void LoadImage(IImageLoaderTask task);
    }
}
