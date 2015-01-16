using Android.Widget;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using System;
using HGR.Mobile.Droid.ImageLoading.Views;

namespace HGR.Mobile.Droid.ImageLoading.Work
{
    public static class ImageService
    {
        private static readonly ImageWorker _worker;

        static ImageService()
        {
            _worker = new ImageWorker();
        }

        /// <summary>
        /// Constructs a new TaskParameter to load an image from a file.
        /// </summary>
        /// <returns>The new TaskParameter.</returns>
        /// <param name="filepath">Path to the file.</param>
        public static TaskParameter LoadFile(string filepath)
        {
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
                return _worker.ExitTasksEarly;
            }
        }

        /// <summary>
        /// Sets a value indicating whether ImageService will exit tasks earlier
        /// </summary>
        /// <param name="exitTasksEarly">If set to <c>true</c> exit tasks early.</param>
        public static void SetExitTasksEarly(bool exitTasksEarly)
        {
            _worker.SetExitTasksEarly(exitTasksEarly);
        }

        /// <summary>
        /// Sets a value indicating if all loading work should be paused (silently canceled).
        /// </summary>
        /// <param name="pauseWork">If set to <c>true</c> pause/cancel work.</param>
        public static void SetPauseWork(bool pauseWork)
        {
            _worker.SetPauseWork(pauseWork);
        }

        /// <summary>
        /// Cancel any loading work for the given ImageView
        /// </summary>
        /// <param name="imageView">Image view.</param>
        public static void CancelWorkFor(ImageView imageView)
        {
            _worker.Cancel(imageView);
        }

        /// <summary>
        /// Removes a pending image loading task from the work queue.
        /// </summary>
        /// <param name="task">Image loading task to remove.</param>
        public static void RemovePendingTask(ImageLoaderTask task)
        {
            _worker.RemovePendingTask(task);
        }

        /// <summary>
        /// Queue an image loading task.
        /// </summary>
        /// <param name="key">Key used for caching.</param>
        /// <param name="task">Image loading task.</param>
        /// <param name="imageView">Image view that will receive the loaded image.</param>
        public static void LoadImage(string key, ImageLoaderTask task, ImageView imageView)
        {
            _worker.LoadImage(key, task, imageView);
        }
    }
}