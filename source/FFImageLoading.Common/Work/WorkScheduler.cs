using System;
using FFImageLoading.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using FFImageLoading.Cache;
using FFImageLoading.Concurrency;
using System.Diagnostics;

namespace FFImageLoading.Work
{
    public interface IWorkScheduler
    {
        /// <summary>      
        /// Cancels any pending work for the task.        
        /// </summary>        
        /// <param name="task">Image loading task to cancel</param>
        void Cancel(IImageLoaderTask task);

        /// <summary>
        /// Cancels tasks that match predicate.
        /// </summary>
        /// <param name="predicate">Predicate for finding relevant tasks to cancel.</param>
        void Cancel(Func<IImageLoaderTask, bool> predicate);

        bool ExitTasksEarly { get; }

        void SetExitTasksEarly(bool exitTasksEarly);

        void SetPauseWork(bool pauseWork);

        void RemovePendingTask(IImageLoaderTask task);

        /// <summary>      
        /// Schedules the image loading. If image is found in cache then it returns it, otherwise it loads it.        
        /// </summary>        
        /// <param name="key">Key for cache lookup.</param>       
        /// <param name="task">Image loading task.</param>
        void LoadImage(IImageLoaderTask task);
    }

    public class WorkScheduler : IWorkScheduler
    {
        protected class PendingTask
        {
            public int Position { get; set; }

            public IImageLoaderTask ImageLoadingTask { get; set; }

            public Task FrameworkWrappingTask { get; set; }
        }

        readonly IMiniLogger _logger;
        readonly int _maxParallelTasks;
        readonly object _pendingTasksLock = new object();
        readonly List<PendingTask> _pendingTasks = new List<PendingTask>();
        readonly Dictionary<string, PendingTask> _currentlyRunning = new Dictionary<string, PendingTask>();
        Task _dispatch = Task.FromResult<byte>(1);

        IPlatformPerformance _performance;
        bool _verbosePerformanceLogging;
        bool _pauseWork; // volatile?
        int _currentPosition; // useful?
        int _statsTotalPending;
        int _statsTotalRunning;
        int _statsTotalMemoryCacheHits;
        int _statsTotalWaiting;
        long _loadCount;

        public WorkScheduler(IMiniLogger logger, bool verbosePerformanceLogging, IPlatformPerformance performance)
        {
            _verbosePerformanceLogging = verbosePerformanceLogging;
            _logger = logger;
            _performance = performance;

            int _processorCount = Environment.ProcessorCount;
            if (_processorCount <= 2)
                _maxParallelTasks = 1;
            else
                _maxParallelTasks = (int)Math.Truncate((double)_processorCount / 2d) + 1;
        }

        public void Cancel(IImageLoaderTask task)
        {
            try
            {
                if (task != null && !task.IsCancelled && !task.Completed)
                {
                    task.Cancel();
                }
            }
            catch (Exception e)
            {
                _logger.Error("Exception occurent trying to cancel the task", e);
            }
            finally
            {
                if (task != null && task.IsCancelled)
                    task.Parameters.Dispose(); // this will ensure we don't keep a reference due to callbacks
            }
        }

        /// <summary>
        /// Cancels tasks that match predicate.
        /// </summary>
        /// <param name="predicate">Predicate for finding relevant tasks to cancel.</param>
        public void Cancel(Func<IImageLoaderTask, bool> predicate)
        {
            lock (_pendingTasksLock)
            {
                foreach (var task in _pendingTasks.Where(p => predicate(p.ImageLoadingTask)).ToList()) // FMT: here we need a copy since cancelling will trigger them to be removed, hence collection is modified during enumeration
                {
                    task.ImageLoadingTask.Cancel();
                }
            }
        }

        public bool ExitTasksEarly { get; private set; }

        public void SetExitTasksEarly(bool exitTasksEarly)
        {
            ExitTasksEarly = exitTasksEarly;
            SetPauseWork(false);
        }

        public void SetPauseWork(bool pauseWork)
        {
            if (_pauseWork == pauseWork)
                return;

            _pauseWork = pauseWork;

            if (pauseWork)
            {
                _logger.Debug("SetPauseWork paused.");

                lock (_pendingTasksLock)
                {
                    foreach (var task in _pendingTasks.ToList()) // FMT: here we need a copy since cancelling will trigger them to be removed, hence collection is modified during enumeration
                        task.ImageLoadingTask.Cancel();

                    _pendingTasks.Clear();
                }
            }

            if (!pauseWork)
            {
                _logger.Debug("SetPauseWork released.");
            }
        }

        public void RemovePendingTask(IImageLoaderTask task)
        {
            lock (_pendingTasksLock)
            {
                _pendingTasks.RemoveAll(p => p.ImageLoadingTask == task);
            }
        }

		/// <summary>
		/// Schedules the image loading. If image is found in cache then it returns it, otherwise it loads it.
		/// </summary>
		/// <param name="task">Image loading task.</param>
		public async void LoadImage(IImageLoaderTask task)
		{
			Interlocked.Increment(ref _loadCount);

			if (_verbosePerformanceLogging && (_loadCount % 10) == 0)
			{
				LogSchedulerStats();
			}

			if (task == null)
				return;

			if (task.IsCancelled)
			{
				task.Parameters?.Dispose(); // this will ensure we don't keep a reference due to callbacks
				return;
			}

			if (task.Parameters.DelayInMs != null && task.Parameters.DelayInMs > 0)
			{
				await Task.Delay(task.Parameters.DelayInMs.Value).ConfigureAwait(false);
			}

			// If we have the image in memory then it's pointless to schedule the job: just display it straight away
			if (task.CanUseMemoryCache())
			{
				var cacheResult = await task.TryLoadingFromCacheAsync().ConfigureAwait(false);
				if (cacheResult == CacheResult.Found) // If image is loaded from cache there is nothing to do here anymore
				{
					Interlocked.Increment(ref _statsTotalMemoryCacheHits);
				}

				if (cacheResult == CacheResult.Found || cacheResult == CacheResult.ErrorOccured) // if something weird happened with the cache... error callback has already been called, let's just leave
				{
					if (task.Parameters.OnFinish != null)
						task.Parameters.OnFinish(task);
					task.Dispose();
					return;
				}
			}
            else if (task?.Parameters?.Source != ImageSource.Stream && string.IsNullOrWhiteSpace(task?.Parameters?.Path))
			{
				_logger.Debug("ImageService: null path ignored");
				return;
			}

            _dispatch = _dispatch.ContinueWith(async t =>
            {
                try
                {
                    await LoadImageAsync(task).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Error("An error occured while loading image", ex);
                }
            });
        }

        private async Task LoadImageAsync(IImageLoaderTask task)
        {
            if (task.IsCancelled)
            {
                task.Parameters?.Dispose(); // this will ensure we don't keep a reference due to callbacks
                return;
            }

            if (!task.Parameters.Preload)
            {
                lock (_pendingTasksLock)
                {
                    foreach (var pendingTask in _pendingTasks.ToList()) // FMT: here we need a copy since cancelling will trigger them to be removed, hence collection is modified during enumeration
                    {
                        if (pendingTask.ImageLoadingTask != null && pendingTask.ImageLoadingTask.UsesSameNativeControl(task))
                            pendingTask.ImageLoadingTask.CancelIfNeeded();
                    }
                }
            }

            bool loadedFromCache = await task.PrepareAndTryLoadingFromCacheAsync().ConfigureAwait(false);
            if (loadedFromCache)
            {
                if (task.Parameters.OnFinish != null)
                    task.Parameters.OnFinish(task);

                task.Dispose();
                return; // image successfully loaded from cache
            }

            if (task.IsCancelled || _pauseWork)
            {
                task.Parameters?.Dispose(); // this will ensure we don't keep a reference due to callbacks
                return;
            }

            QueueAndGenerateImage(task);
        }

        private PendingTask FindSimilarPendingTask(IImageLoaderTask task)         {             // At first check if the exact same items exists in pending tasks (exact same means same transformations, same downsample, ...)             // Since it will be exactly the same it can be retrieved from memory cache
             string key = task.GetKey(raw: false);             var alreadyRunningTaskForSameKey = _pendingTasks.FirstOrDefault(t => t.ImageLoadingTask.GetKey(raw: false) == key);

            return alreadyRunningTaskForSameKey;         }

        private void QueueAndGenerateImage(IImageLoaderTask task)
        {
            _logger.Debug(string.Format("Generating/retrieving image: {0}", task.GetKey()));

            int position = Interlocked.Increment(ref _currentPosition);
            var currentPendingTask = new PendingTask() { Position = position, ImageLoadingTask = task, FrameworkWrappingTask = CreateFrameworkTask(task) };

            PendingTask alreadyRunningTaskForSameKey = null;
            lock (_pendingTasksLock)
            {
                alreadyRunningTaskForSameKey = FindSimilarPendingTask(task);
                if (alreadyRunningTaskForSameKey == null)
                {
                    Interlocked.Increment(ref _statsTotalPending);
                    _pendingTasks.Add(currentPendingTask);
                }
                else
                {
                    alreadyRunningTaskForSameKey.Position = position;
                }
            }

            if (alreadyRunningTaskForSameKey == null || !currentPendingTask.ImageLoadingTask.CanUseMemoryCache())
            {
                Run(currentPendingTask);
            }
            else
            {
                WaitForSimilarTask(currentPendingTask, alreadyRunningTaskForSameKey);
            }
        }

        private async void WaitForSimilarTask(PendingTask currentPendingTask, PendingTask alreadyQueuedTaskForSameKey)
        {
            string queuedKey = alreadyQueuedTaskForSameKey.ImageLoadingTask.GetKey();
            Interlocked.Increment(ref _statsTotalWaiting);

            Action forceQueue = () =>
            {
                lock (_pendingTasksLock)
                {
                    Interlocked.Increment(ref _statsTotalPending);
                    _pendingTasks.Add(currentPendingTask);
                }
                Run(currentPendingTask);
            };

            if (alreadyQueuedTaskForSameKey.FrameworkWrappingTask == null)
            {
                _logger.Debug(string.Format("No C# Task defined for key: {0}", queuedKey));
                forceQueue();
                return;
            }

            _logger.Debug(string.Format("Wait for similar request for key: {0}", queuedKey));
            // This will wait for the pending task or if it is already finished then it will just pass
            await alreadyQueuedTaskForSameKey.FrameworkWrappingTask.ConfigureAwait(false);

            // Now our image should be in the cache
            var cacheResult = await currentPendingTask.ImageLoadingTask.TryLoadingFromCacheAsync().ConfigureAwait(false);
            if (cacheResult != CacheResult.Found)
            {
                _logger.Debug(string.Format("Similar request finished but the image is not in the cache: {0}", queuedKey));
                forceQueue();
                return;
            }
            else
            {
                var task = currentPendingTask.ImageLoadingTask;
                if (task.Parameters.OnFinish != null)
                    task.Parameters.OnFinish(task);

                task.Dispose();
            }
        }

        private async void Run(PendingTask pendingTask)
        {
            await RunAsync().ConfigureAwait(false); // FMT: we limit concurrent work using MaxParallelTasks
        }

        private Task CreateFrameworkTask(IImageLoaderTask imageLoadingTask)
        {
            var parameters = imageLoadingTask.Parameters;

            var tcs = new TaskCompletionSource<bool>();

            var successCallback = parameters.OnSuccess;
            parameters.Success((size, result) =>
            {
                tcs.TrySetResult(true);

                if (successCallback != null)
                    successCallback(size, result);
            });

            var finishCallback = parameters.OnFinish;
            parameters.Finish(sw =>
            {
                tcs.TrySetResult(false);

                if (finishCallback != null)
                    finishCallback(sw);
            });

            return tcs.Task;
        }

        private async Task RunAsync()
        {
            Dictionary<string, PendingTask> currentLotOfPendingTasks = null;

            lock (_pendingTasksLock)
            {
                if (_currentlyRunning.Count >= _maxParallelTasks)
                    return;

                int numberOfTasks = _maxParallelTasks - _currentlyRunning.Count;
                if (numberOfTasks > 0)
                {
                    currentLotOfPendingTasks = new Dictionary<string, PendingTask>();


                    foreach (var task in _pendingTasks
                                .Where(t => !t.ImageLoadingTask.IsCancelled && !t.ImageLoadingTask.Completed)
                                .OrderByDescending(t => t.ImageLoadingTask.Parameters.Priority ?? 0)
                                .ThenBy(t => t.Position))
                    {
                        // We don't want to load, at the same time, images that have same key or same raw key at the same time
                        // This way we prevent concurrent downloads and benefit from caches
                        string key = task.ImageLoadingTask.GetKey();
                        if (!_currentlyRunning.ContainsKey(key) && !currentLotOfPendingTasks.ContainsKey(key))
                        {
                            string rawKey = task.ImageLoadingTask.GetKey(raw: true);
                            if (!_currentlyRunning.ContainsKey(rawKey) && !currentLotOfPendingTasks.ContainsKey(rawKey))
                            {
                                currentLotOfPendingTasks.Add(key, task);

                                if (currentLotOfPendingTasks.Count == numberOfTasks)
                                    break;
                            }
                        }
                    }
                }
            }

            if (currentLotOfPendingTasks == null || currentLotOfPendingTasks.Count == 0)
            {
                return; // FMT: no need to do anything else
            }

            if (currentLotOfPendingTasks.Count == 1)
            {
                await QueueTaskAsync(currentLotOfPendingTasks.Values.First(), false).ConfigureAwait(false);
            }
            else
            {
                var tasks = currentLotOfPendingTasks.Select(p => QueueTaskAsync(p.Value, true));
                await Task.WhenAny(tasks).ConfigureAwait(false);
            }
        }

        private async Task QueueTaskAsync(PendingTask pendingTask, bool scheduleOnThreadPool)
        {
            lock (_pendingTasksLock)
            {
                if (_currentlyRunning.Count >= _maxParallelTasks)
                    return;
            }

            string key = pendingTask.ImageLoadingTask.GetKey();

            try
            {
                PendingTask alreadyRunningTask = null;

                lock (_pendingTasksLock)
                {
                    if (!_currentlyRunning.ContainsKey(key))
                    {
                        _currentlyRunning.Add(key, pendingTask);
                        Interlocked.Increment(ref _statsTotalRunning);
                    }
                    else
                    {
                        alreadyRunningTask = _currentlyRunning[key];

                        // duplicate - return
                        if (pendingTask == alreadyRunningTask)
                            return;
                    }
                }

                if (alreadyRunningTask != null)
                {
                    WaitForSimilarTask(pendingTask, alreadyRunningTask);
                    return;
                }

                if (_verbosePerformanceLogging)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    if (scheduleOnThreadPool)
                    {
                        await Task.Run(pendingTask.ImageLoadingTask.RunAsync).ConfigureAwait(false);
                    }
                    else
                    {
                        await pendingTask.ImageLoadingTask.RunAsync().ConfigureAwait(false);
                    }

                    stopwatch.Stop();

                    LogSchedulerStats();
                    _logger.Debug(string.Format("[PERFORMANCE] RunAsync - NetManagedThreadId: {0}, NativeThreadId: {1}, Execution: {2} ms, ThreadPool: {3}, Key: {4}",
                                                _performance.GetCurrentManagedThreadId(),
                                                _performance.GetCurrentSystemThreadId(),
                                                stopwatch.Elapsed.Milliseconds,
                                                scheduleOnThreadPool,
                                                key));
                }
                else
                {
                    if (scheduleOnThreadPool)
                    {
                        await Task.Run(pendingTask.ImageLoadingTask.RunAsync).ConfigureAwait(false);
                    }
                    else
                    {
                        await pendingTask.ImageLoadingTask.RunAsync().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                lock (_pendingTasksLock)
                {
                    _currentlyRunning.Remove(key);
                }

                await RunAsync().ConfigureAwait(false);
            }
        }

        void LogSchedulerStats()
        {
            _logger.Debug(string.Format("[PERFORMANCE] Scheduler - Max: {0}, Pending: {1}, Running: {2}, TotalPending: {3}, TotalRunning: {4}, TotalMemoryCacheHit: {5}, TotalWaiting: {6}",
                                         _maxParallelTasks,
                                         _pendingTasks.Count,
                                         _currentlyRunning.Count,
                                         _statsTotalPending,
                                         _statsTotalRunning,
                                         _statsTotalMemoryCacheHits,
                                         _statsTotalWaiting));
            
            _logger.Debug(_performance.GetMemoryInfo());
        }
    }
}
