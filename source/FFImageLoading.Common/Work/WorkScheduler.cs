using System;
using FFImageLoading.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace FFImageLoading.Work
{
	public interface IWorkScheduler
	{
		/// <summary>
		/// Cancels any pending work for the task.
		/// </summary>
		/// <param name="task">Image loading task to cancel</param>
		void Cancel(IImageLoaderTask task);

		bool ExitTasksEarly { get; }

		Task SetExitTasksEarlyAsync(bool exitTasksEarly);

		Task SetPauseWorkAsync(bool pauseWork);

		Task RemovePendingTaskAsync(IImageLoaderTask task);

		/// <summary>
		/// Schedules the image loading. If image is found in cache then it returns it, otherwise it loads it.
		/// </summary>
		/// <param name="key">Key for cache lookup.</param>
		/// <param name="task">Image loading task.</param>
		void LoadImage(IImageLoaderTask task);
	}

	public class WorkScheduler: IWorkScheduler
	{
		protected class PendingTask
		{
			public int Position { get; set; }

			public IImageLoaderTask ImageLoadingTask { get; set; }

			public Task FrameworkWrappingTask { get; set; }
		}

		private readonly IMiniLogger _logger;
		private readonly int _defaultParallelTasks;
		private readonly List<PendingTask> _pendingTasks;
        private List<PendingTask> _currentlyRunning;
        private readonly SemaphoreSlim _pauseWorkLock;
        private readonly SemaphoreSlim _pendingTasksLock;
        private readonly SemaphoreSlim _runningLock;

		private bool _exitTasksEarly;
		private bool _pauseWork;
		private int _currentPosition;

		public WorkScheduler(IMiniLogger logger)
		{
			_logger = logger;
			_pauseWorkLock = new SemaphoreSlim(1);
            _pendingTasksLock = new SemaphoreSlim(1);
            _runningLock = new SemaphoreSlim(1);
			_pendingTasks = new List<PendingTask>();
            _currentlyRunning = new List<PendingTask>();

			int _processorCount = Environment.ProcessorCount;
			if (_processorCount <= 2)
				_defaultParallelTasks = 1;
			else
				_defaultParallelTasks = (int)Math.Truncate ((double)_processorCount / 2d) + 1;
		}

		public virtual int MaxParallelTasks
		{
			get
			{
				return _defaultParallelTasks;
			}
		}

		/// <summary>
		/// Cancels any pending work for the task.
		/// </summary>
		/// <param name="task">Image loading task to cancel</param>
		/// <returns><c>true</c> if this instance cancel task; otherwise, <c>false</c>.</returns>
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

		public bool ExitTasksEarly
		{
			get
			{
				return _exitTasksEarly;
			}
		}

        public async Task SetExitTasksEarlyAsync(bool exitTasksEarly)
		{
			_exitTasksEarly = exitTasksEarly;
            await SetPauseWorkAsync(false).ConfigureAwait(false);
		}

        public async Task SetPauseWorkAsync(bool pauseWork)
		{
            await _pauseWorkLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_pauseWork == pauseWork)
                    return;

                _pauseWork = pauseWork;

                if (pauseWork)
                {
                    _logger.Debug("SetPauseWork paused.");

                    List<PendingTask> pendingTasksCopy;
                    await _pendingTasksLock.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        pendingTasksCopy = _pendingTasks.ToList(); // we iterate on a copy
                    }
                    finally
                    {
                        _pendingTasksLock.Release();
                    }

                    foreach (var task in pendingTasksCopy)
                        task.ImageLoadingTask.Cancel();

                    await _pendingTasksLock.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        _pendingTasks.Clear();
					}
                    finally
                    {
                        _pendingTasksLock.Release();
                    }
				}

				if (!pauseWork)
				{
					_logger.Debug("SetPauseWork released.");
				}
			}
            finally
            {
                _pauseWorkLock.Release();
            }
		}

        public async Task RemovePendingTaskAsync(IImageLoaderTask task)
		{
            await _pauseWorkLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await _pendingTasksLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    var pendingTask = _pendingTasks.FirstOrDefault(t => t.ImageLoadingTask == task);
                    if (pendingTask != null)
                        _pendingTasks.Remove(pendingTask);
                }
                finally
                {
                    _pendingTasksLock.Release();
                }
			}
            finally
            {
                _pauseWorkLock.Release();
            }
		}

		/// <summary>
		/// Schedules the image loading. If image is found in cache then it returns it, otherwise it loads it.
		/// </summary>
		/// <param name="task">Image loading task.</param>
		public void LoadImage(IImageLoaderTask task)
		{
			if (task == null)
				return;

			#pragma warning disable 4014
			Task.Run(async () =>
			{
				if (task.Parameters.DelayInMs != null && task.Parameters.DelayInMs > 0)
				{
					await Task.Delay(task.Parameters.DelayInMs.Value).ConfigureAwait(false);
				}

				if (task.IsCancelled)
				{
					task.Parameters.Dispose(); // this will ensure we don't keep a reference due to callbacks
					return;
				}
				
				if (!task.Parameters.Preload)
				{
					List<PendingTask> pendingTasksCopy;
                    await _pendingTasksLock.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        pendingTasksCopy = _pendingTasks.ToList();
                    }
                    finally
                    {
                        _pendingTasksLock.Release();
                    }

					foreach (var pendingTask in pendingTasksCopy)
					{
						if (pendingTask.ImageLoadingTask != null && pendingTask.ImageLoadingTask.UsesSameNativeControl(task))
							pendingTask.ImageLoadingTask.CancelIfNeeded();
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
					task.Parameters.Dispose(); // this will ensure we don't keep a reference due to callbacks
					return;
				}

                await QueueAndGenerateImageAsync(task).ConfigureAwait(false);
			});
			#pragma warning restore 4014
		}

        private async Task QueueAndGenerateImageAsync(IImageLoaderTask task)
		{
			_logger.Debug(string.Format("Generating/retrieving image: {0}", task.GetKey()));

			int position = Interlocked.Increment(ref _currentPosition);
			var currentPendingTask = new PendingTask() { Position = position, ImageLoadingTask = task };
			PendingTask alreadyRunningTaskForSameKey = null;
            await _pauseWorkLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await _pendingTasksLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    alreadyRunningTaskForSameKey = _pendingTasks.FirstOrDefault(t => t.ImageLoadingTask.GetKey() == task.GetKey() && (!t.ImageLoadingTask.IsCancelled));
                    if (alreadyRunningTaskForSameKey == null)
                        _pendingTasks.Add(currentPendingTask);
                    else
                        alreadyRunningTaskForSameKey.Position = position;
                }
                finally
                {
                    _pendingTasksLock.Release();
                }
			}
            finally
            {
                _pauseWorkLock.Release();
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

		private async void WaitForSimilarTask(PendingTask currentPendingTask, PendingTask alreadyRunningTaskForSameKey)
		{
			string key = alreadyRunningTaskForSameKey.ImageLoadingTask.GetKey();

			Action forceLoad = async () =>
			{
                await _pauseWorkLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    await _pendingTasksLock.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        _pendingTasks.Add(currentPendingTask);
                    }
                    finally
                    {
                        _pendingTasksLock.Release();
                    }
                }
                finally
                {
                    _pauseWorkLock.Release();
                }

				Run(currentPendingTask);
			};

			if (alreadyRunningTaskForSameKey.FrameworkWrappingTask == null)
			{
				_logger.Debug(string.Format("No C# Task defined for key: {0}", key));
				forceLoad();
				return;
			}

			_logger.Debug(string.Format("Wait for similar request for key: {0}", key));
			// This will wait for the pending task or if it is already finished then it will just pass
			await alreadyRunningTaskForSameKey.FrameworkWrappingTask.ConfigureAwait(false);

			// Now our image should be in the cache
			var cacheResult = await currentPendingTask.ImageLoadingTask.TryLoadingFromCacheAsync().ConfigureAwait(false);
			if (cacheResult != FFImageLoading.Cache.CacheResult.Found)
			{
				_logger.Debug(string.Format("Similar request finished but the image is not in the cache: {0}", key));
				forceLoad();
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
			if (MaxParallelTasks <= 0)
			{
				pendingTask.FrameworkWrappingTask = pendingTask.ImageLoadingTask.RunAsync(); // FMT: threadpool will limit concurrent work
				await pendingTask.FrameworkWrappingTask.ConfigureAwait(false);
				return;
			}

			var tcs = new TaskCompletionSource<bool>();

			var successCallback = pendingTask.ImageLoadingTask.Parameters.OnSuccess;
			pendingTask.ImageLoadingTask.Parameters.Success((size, result) =>
			{
				tcs.TrySetResult(true);

				if (successCallback != null)
					successCallback(size, result);
			});

			var finishCallback = pendingTask.ImageLoadingTask.Parameters.OnFinish;
			pendingTask.ImageLoadingTask.Parameters.Finish(sw =>
			{
				tcs.TrySetResult(false);

				if (finishCallback != null)
					finishCallback(sw);
			});

			pendingTask.FrameworkWrappingTask = tcs.Task;
			await RunAsync().ConfigureAwait(false); // FMT: we limit concurrent work using MaxParallelTasks
		}

		private async Task RunAsync()
		{
            int runningCount = await RunningCountAsync().ConfigureAwait(false);
            if (runningCount >= MaxParallelTasks)
            {
                return;
            }

            List<PendingTask> currentLotOfPendingTasks = null;
            await _pauseWorkLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await _pendingTasksLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    runningCount = await RunningCountAsync().ConfigureAwait(false);
                    int numberOfTasks = MaxParallelTasks - runningCount;
                    if (numberOfTasks > 0)
                    {
                        currentLotOfPendingTasks = _pendingTasks
                            .Where(t => !t.ImageLoadingTask.IsCancelled && !t.ImageLoadingTask.Completed)
                            .OrderByDescending(t => t.ImageLoadingTask.Parameters.Priority)
                            .ThenBy(t => t.Position)
                            .Take(numberOfTasks)
                            .ToList();
                    }
                }
                finally
                {
                    _pendingTasksLock.Release();
                }

                if (currentLotOfPendingTasks == null || currentLotOfPendingTasks.Count == 0)
                {
                    return; // FMT: no need to do anything else
                }
            }
            finally
            {
                _pauseWorkLock.Release();
            }

            if (currentLotOfPendingTasks.Count == 1)
            {
                await QueueTaskAsync(currentLotOfPendingTasks[0], false).ConfigureAwait(false);
            }
            else
            {
                var tasks = currentLotOfPendingTasks.Select(p => QueueTaskAsync(p, true));
                await Task.WhenAny(tasks).ConfigureAwait(false);
            }

            RunAndForget();
		}

        private async void RunAndForget()
        {
            await RunAsync().ConfigureAwait(false);
        }

        private async Task<int> RunningCountAsync()
        {
            await _runningLock.WaitAsync().ConfigureAwait(false);
            try
            {
                return _currentlyRunning.Count;
            }
            finally
            {
                _runningLock.Release();
            }
        }

        private async Task QueueTaskAsync(PendingTask pendingTask, bool scheduleOnThreadPool)
        {
            try
            {
                await _runningLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (_currentlyRunning.Count >= MaxParallelTasks)
                        return;

                    _currentlyRunning.Add(pendingTask);
                }
                finally
                {
                    _runningLock.Release();
                }

                if (scheduleOnThreadPool)
                {
                    await Task.Run(pendingTask.ImageLoadingTask.RunAsync).ConfigureAwait(false);
                }
                else
                {
                    await pendingTask.ImageLoadingTask.RunAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await _runningLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    _currentlyRunning.Remove(pendingTask);
                }
                finally
                {
                    _runningLock.Release();
                }

            }
        }
	}
}

