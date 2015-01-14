using System;
using System.Threading;
using Android.Content.Res;
using Android.OS;
using Android.Widget;
using Java.Lang;
using Java.Util.Concurrent;
using Exception = System.Exception;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HGR.Mobile.Droid.ImageLoading.Helpers;
using HGR.Mobile.Droid.ImageLoading.Extensions;
using HGR.Mobile.Droid.ImageLoading.Cache;

namespace HGR.Mobile.Droid.ImageLoading.Work
{
    public interface IImageWorkerBase<TKey>
    {
        /// <summary>
        /// Cancels any pending work attached to the provided ImageView.
        /// </summary>
        /// <param name="imageView"></param>
        void Cancel(ImageView imageView);

        /// <summary>
        /// Returns true if the current work has been canceled or if there was no work in progress on this image view. Returns
        /// false if the work in progress deals with the same data. The work is not stopped in that case.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="imageView"></param>
        /// <returns></returns>
        bool CancelPotentialWork(string path, ImageView imageView);


        void LoadImage(TKey key, ImageView imageView, Action action);
    }

    public abstract class ImageWorkerBase<TKey> : IImageWorkerBase<TKey>
    {
        private static int _defaultParallelTasks;

        protected readonly Resources _resources;
        private const bool _useFadeInBitmap = true;
        private bool _isRunning;
        private readonly object _runningLock = new object();

        static ImageWorkerBase()
        {
            int _processorCount = Runtime.GetRuntime().AvailableProcessors();
            if (_processorCount == 1)
                _defaultParallelTasks = 1;
            else
                _defaultParallelTasks = (int)System.Math.Truncate((double)_processorCount / 2);
        }

        protected ImageWorkerBase()
        {
            _resources = Android.App.Application.Context.ApplicationContext.Resources;
        }

        public virtual int MaxParallelTasks
        {
            get
            {
                return _defaultParallelTasks;
            }
        }

        public static void SetExitTaskEarly(bool exitTasksEarly)
        {
            ImageWorkerService.ExitTasksEarly = exitTasksEarly;
            SetPauseWork(false);
        }

        public static void SetPauseWork(bool pauseWork)
        {
            lock (ImageWorkerService.PauseWorkLock)
            {
                if (ImageWorkerService.PauseWork == pauseWork)
                    return;

                ImageWorkerService.PauseWork = pauseWork;

                if (pauseWork)
                {
                    MiniLogger.Debug("SetPauseWork paused.");
                    ImageWorkerService.PendingTasks.ForEach(t => t.Cancel());
                    ImageWorkerService.PendingTasks.Clear();
                }

                if (!pauseWork)
                {
                    MiniLogger.Debug("SetPauseWork released.");
                }
            }
        }

        public static void RemovePendingTask(ImageLoaderTask task)
        {
            lock (ImageWorkerService.PauseWorkLock)
            {
                ImageWorkerService.PendingTasks.Remove(task);
            }
        }

        public abstract ImageLoaderTask GetLoaderTask(TKey path, ImageView imageView);

        /// <summary>
        /// Cancels any pending work attached to the provided ImageView.
        /// </summary>
        /// <param name="imageView"></param>
        public void Cancel(ImageView imageView)
        {
            var task = imageView.GetImageLoaderTask();
            try
            {
                if (task != null && !task.IsCancelled && !task.Completed)
                {
                    task.Cancel();
                }
            }
            catch (Exception e)
            {
                MiniLogger.Error("Exception occurent trying to cancel the task", e);
            }
        }

        /// <summary>
        /// Returns true if the current work has been canceled or if there was no work in progress on this image view. Returns
        /// false if the work in progress deals with the same data. The work is not stopped in that case.
        /// </summary>
        /// <param name="key"></param>
        /// ;
        /// <param name="imageView"></param>
        /// <returns></returns>
        public bool CancelPotentialWork(string key, ImageView imageView)
        {
            var imageLoaderTask = imageView.GetImageLoaderTask();
            if (imageLoaderTask != null && !imageLoaderTask.IsCancelled)
            {
                var bitmapPath = imageLoaderTask.Key;
                if (string.IsNullOrWhiteSpace(bitmapPath) || !bitmapPath.Equals(key))
                {
                    // Cancel previous task
                    imageLoaderTask.Cancel();
                }
                else
                {
                    // The same work is already in progress
                    return false;
                }
            }
            // No task associated with the ImageView, or an existing task was cancelled
            return true;
        }

        public void LoadImage(TKey key, ImageView imageView, Action action)
        {
            if (key.Equals(default(TKey)))
                return;

            Action onComplete = () =>
            {
                if (action != null)
                    action();
            };

            var value = ImageCache.Instance.Get(key.ToString());
            if (value != null)
            {
                MiniLogger.Debug(string.Format("Image from cache: {0}", key));
                // Bitmap found in memory cache
                imageView.SetImageDrawable(value);
                imageView.LayoutParameters.Height = value.IntrinsicHeight;
                imageView.LayoutParameters.Width = value.IntrinsicWidth;

                onComplete();
            }
            else if (CancelPotentialWork(key.ToString(), imageView))
            {
                if (ImageWorkerService.PauseWork)
                    return;

                var task = GetLoaderTask(key, imageView);
                task.OnComplete += (s, e) => onComplete();

                var asyncDrawable = new AsyncDrawable(_resources, null, task);
                imageView.SetImageDrawable(asyncDrawable);

                MiniLogger.Debug(string.Format("Generating/retrieving image: {0}", key));

                lock (ImageWorkerService.PauseWorkLock)
                {
                    ImageWorkerService.PendingTasks.Add(task);
                }

                Run(task);
            }
        }

        private async void Run(ImageLoaderTask imageLoaderTask)
        {
            if (MaxParallelTasks <= 0)
            {
                await imageLoaderTask.RunAsync().ConfigureAwait(false); // FMT: threadpool will limit concurrent work
                return;
            }

            await RunAsync().ConfigureAwait(false); // FMT: we limit concurrent work using MaxParallelTasks
        }

        private async Task RunAsync()
        {
            lock (_runningLock)
            {
                if (_isRunning)
                    return;
                _isRunning = true;
            }

            List<ImageLoaderTask> imageLoaderTasks = null;
            lock (ImageWorkerService.PauseWorkLock)
            {
                imageLoaderTasks = ImageWorkerService.PendingTasks.Where(t => !t.IsCancelled && !t.Completed).Take(MaxParallelTasks).ToList();
                if (imageLoaderTasks.Count == 0)
                {
                    lock (_runningLock)
                    {
                        _isRunning = false;
                        return; // FMT: no need to do anything else
                    }
                }
            }

            var tasks = imageLoaderTasks.Select(t => t.RunAsync());
            await Task.WhenAll(tasks).ConfigureAwait(false);
            
            lock (_runningLock)
            {
                _isRunning = false;
            }

            await RunAsync().ConfigureAwait(false);
        }
    }
}