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
    public interface IImageWorker
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

        void LoadImage(string key, ImageLoaderTask task, ImageView imageView);
    }

    public class ImageWorker : IImageWorker
    {
        private class PendingTask
        {
            public ImageLoaderTask ImageLoadingTask { get; set; }
            public Task FrameworkWrappingTask { get; set; }
        }

        private const bool _useFadeInBitmap = true;

        private readonly Resources _resources;
        private readonly int _defaultParallelTasks;
        private readonly object _pauseWorkLock = new object();
        private readonly List<PendingTask> _pendingTasks = new List<PendingTask>();
        private readonly object _runningLock = new object();

        private bool _exitTasksEarly;
        private bool _pauseWork;
        private bool _isRunning;

        internal ImageWorker()
        {
            _resources = Android.App.Application.Context.ApplicationContext.Resources;

            int _processorCount = Runtime.GetRuntime().AvailableProcessors();
            if (_processorCount == 1)
                _defaultParallelTasks = 1;
            else
                _defaultParallelTasks = (int)System.Math.Truncate((double)_processorCount / 2);
        }

        public virtual int MaxParallelTasks
        {
            get
            {
                return _defaultParallelTasks;
            }
        }

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

        public bool ExitTasksEarly
        {
            get
            {
                return _exitTasksEarly;
            }
        }

        public void SetExitTasksEarly(bool exitTasksEarly)
        {
            _exitTasksEarly = exitTasksEarly;
            SetPauseWork(false);
        }

        public void SetPauseWork(bool pauseWork)
        {
            lock (_pauseWorkLock)
            {
                if (_pauseWork == pauseWork)
                    return;

                _pauseWork = pauseWork;

                if (pauseWork)
                {
                    MiniLogger.Debug("SetPauseWork paused.");
                    _pendingTasks.ForEach(t => t.ImageLoadingTask.Cancel());
                    _pendingTasks.Clear();
                }

                if (!pauseWork)
                {
                    MiniLogger.Debug("SetPauseWork released.");
                }
            }
        }

        public void RemovePendingTask(ImageLoaderTask task)
        {
            lock (_pauseWorkLock)
            {
                var pendingTask = _pendingTasks.FirstOrDefault(t => t.ImageLoadingTask == task);
                if (pendingTask != null)
                    _pendingTasks.Remove(pendingTask);
            }
        }

        public void LoadImage(string key, ImageLoaderTask task, ImageView imageView)
        {
            if (string.IsNullOrEmpty(key))
                return;

            var value = ImageCache.Instance.Get(key);
            if (value != null) {
                MiniLogger.Debug(string.Format("Image from cache: {0}", key));
                // Bitmap found in memory cache
                imageView.SetImageDrawable(value);
                imageView.LayoutParameters.Height = value.IntrinsicHeight;
                imageView.LayoutParameters.Width = value.IntrinsicWidth;

                task.Parameters.OnSuccess();
            } else if (CancelPotentialWork(key, imageView)) {
                if (_pauseWork)
                    return;

                var asyncDrawable = new AsyncDrawable(_resources, null, task);
                imageView.SetImageDrawable(asyncDrawable);

                MiniLogger.Debug(string.Format("Generating/retrieving image: {0}", key));

                var currentPendingTask = new PendingTask() { ImageLoadingTask = task };
                PendingTask alreadyRunningTaskForSameKey = null;
                lock (_pauseWorkLock) {
                    alreadyRunningTaskForSameKey = _pendingTasks.FirstOrDefault(t => t.ImageLoadingTask.Key == key);
                    if (alreadyRunningTaskForSameKey == null)
                        _pendingTasks.Add(currentPendingTask);
                }

                if (alreadyRunningTaskForSameKey == null) {
                    Run(currentPendingTask);
                } else {
                    WaitForSimilarTask(currentPendingTask, alreadyRunningTaskForSameKey, imageView);
                }
            }
        }

        private async void WaitForSimilarTask(PendingTask currentPendingTask, PendingTask alreadyRunningTaskForSameKey, ImageView imageView)
        {
            string key = alreadyRunningTaskForSameKey.ImageLoadingTask.Key;

            Action forceLoad = () => {
                lock (_pauseWorkLock) {
                    _pendingTasks.Add(currentPendingTask);
                }

                Run(currentPendingTask);
            };

            if (alreadyRunningTaskForSameKey.FrameworkWrappingTask == null) {
                MiniLogger.Debug("No C# Task defined for key: " + key);
                forceLoad();
                return;
            }

            MiniLogger.Debug("Wait for similar request for key: " + key);
            // This will wait for the pending task or if it is already finished then it will just pass
            await alreadyRunningTaskForSameKey.FrameworkWrappingTask.ConfigureAwait(false);

            // Now our image should be in the cache
            var value = ImageCache.Instance.Get(key);

            if (value != null) {
                imageView.SetImageDrawable(value);
                imageView.LayoutParameters.Height = value.IntrinsicHeight;
                imageView.LayoutParameters.Width = value.IntrinsicWidth;
                alreadyRunningTaskForSameKey.ImageLoadingTask.Parameters.OnSuccess();
            } else {
                MiniLogger.Debug("Similar request finished but the image is not in the cache: " + key);
                forceLoad();
            }
        }

        private async void Run(PendingTask pendingTask)
        {
            if (MaxParallelTasks <= 0)
            {
                pendingTask.FrameworkWrappingTask = pendingTask.ImageLoadingTask.RunAsync(); // FMT: threadpool will limit concurrent work
                await pendingTask.FrameworkWrappingTask.ConfigureAwait(false);
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

            List<PendingTask> currentLotOfPendingTasks = null;
            lock (_pauseWorkLock)
            {
                currentLotOfPendingTasks = _pendingTasks.Where(t => !t.ImageLoadingTask.IsCancelled && !t.ImageLoadingTask.Completed)
                    .Take(MaxParallelTasks)
                    .ToList();
                if (currentLotOfPendingTasks.Count == 0)
                {
                    lock (_runningLock)
                    {
                        _isRunning = false;
                        return; // FMT: no need to do anything else
                    }
                }
            }

            foreach (var pendingTask in currentLotOfPendingTasks)
            {
                pendingTask.FrameworkWrappingTask = pendingTask.ImageLoadingTask.RunAsync();
            }

            var tasks = currentLotOfPendingTasks.Select(t => t.FrameworkWrappingTask);
            await Task.WhenAll(tasks).ConfigureAwait(false);
            
            lock (_runningLock)
            {
                _isRunning = false;
            }

            await RunAsync().ConfigureAwait(false);
        }
    }
}