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

	public class WorkScheduler: IWorkScheduler
	{
		protected class PendingTask
		{
			public IImageLoaderTask ImageLoadingTask { get; set; }

			public Task FrameworkWrappingTask { get; set; }
		}

		private readonly IMiniLogger _logger;
		private readonly int _defaultParallelTasks;
		private readonly object _pauseWorkLock;
		private readonly List<PendingTask> _pendingTasks;
		private readonly object _pendingTasksLock = new object();
		private readonly object _runningLock = new object();

		private bool _exitTasksEarly;
		private bool _pauseWork;
		private bool _isRunning;

		public WorkScheduler(IMiniLogger logger)
		{
			_logger = logger;
			_pauseWorkLock = new object();
			_pendingTasks = new List<PendingTask>();

			int _processorCount = Environment.ProcessorCount;
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
					_logger.Debug("SetPauseWork paused.");

					List<PendingTask> pendingTasksCopy;
					lock (_pendingTasksLock)
					{
						pendingTasksCopy = _pendingTasks.ToList(); // we iterate on a copy
					}

					foreach (var task in pendingTasksCopy)
						task.ImageLoadingTask.Cancel();

					lock (_pendingTasksLock)
					{ 
						_pendingTasks.Clear();
					}
				}

				if (!pauseWork)
				{
					_logger.Debug("SetPauseWork released.");
				}
			}
		}

		public void RemovePendingTask(IImageLoaderTask task)
		{
			lock (_pauseWorkLock)
			{
				lock (_pendingTasksLock)
				{
					var pendingTask = _pendingTasks.FirstOrDefault(t => t.ImageLoadingTask == task);
					if (pendingTask != null)
						_pendingTasks.Remove(pendingTask);
				}
			}
		}

		/// <summary>
		/// Schedules the image loading. If image is found in cache then it returns it, otherwise it loads it.
		/// </summary>
		/// <param name="task">Image loading task.</param>
		public async void LoadImage(IImageLoaderTask task)
		{
			if (task == null)
				return;

			if (task.IsCancelled)
			{
				task.Parameters.Dispose(); // this will ensure we don't keep a reference due to callbacks
				return;
			}

			List<PendingTask> pendingTasksCopy;
			lock (_pendingTasksLock)
			{
				pendingTasksCopy = _pendingTasks.ToList();
			}
			foreach (var pendingTask in pendingTasksCopy)
			{
				if (pendingTask.ImageLoadingTask != null && pendingTask.ImageLoadingTask.UsesSameNativeControl(task))
					pendingTask.ImageLoadingTask.CancelIfNeeded();
			}

			bool loadedFromCache = false;
			if (task.CanUseMemoryCache())
			{
				loadedFromCache = await task.PrepareAndTryLoadingFromCacheAsync().ConfigureAwait(false);
			}

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

			QueueAndGenerateImage(task);
		}

		private void QueueAndGenerateImage(IImageLoaderTask task)
		{
			_logger.Debug(string.Format("Generating/retrieving image: {0}", task.GetKey()));

			var currentPendingTask = new PendingTask() { ImageLoadingTask = task };
			PendingTask alreadyRunningTaskForSameKey = null;
			lock (_pauseWorkLock)
			{
				lock (_pendingTasksLock)
				{
					alreadyRunningTaskForSameKey = _pendingTasks.FirstOrDefault(t => t.ImageLoadingTask.GetKey() == task.GetKey() && (!t.ImageLoadingTask.IsCancelled));
					if (alreadyRunningTaskForSameKey == null)
						_pendingTasks.Add(currentPendingTask);
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

		private async void WaitForSimilarTask(PendingTask currentPendingTask, PendingTask alreadyRunningTaskForSameKey)
		{
			string key = alreadyRunningTaskForSameKey.ImageLoadingTask.GetKey();

			Action forceLoad = () =>
			{
				lock (_pauseWorkLock)
				{
					lock(_pendingTasksLock)
					{
						_pendingTasks.Add(currentPendingTask);
					}
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
			lock (_runningLock)
			{
				if (_isRunning)
					return;
				_isRunning = true;
			}

			List<PendingTask> currentLotOfPendingTasks = null;
			lock (_pauseWorkLock)
			{
				lock (_pendingTasksLock)
				{
					currentLotOfPendingTasks = _pendingTasks.Where(t => !t.ImageLoadingTask.IsCancelled && !t.ImageLoadingTask.Completed)
                    .Take(MaxParallelTasks)
                    .ToList();
				}

				if (currentLotOfPendingTasks.Count == 0)
				{
					lock (_runningLock)
					{
						_isRunning = false;
						return; // FMT: no need to do anything else
					}
				}
			}

			var frameworkTasks = new List<Task>();
			foreach (var pendingTask in currentLotOfPendingTasks)
			{
				frameworkTasks.Add(pendingTask.ImageLoadingTask.RunAsync());
			}

			await Task.WhenAll(frameworkTasks).ConfigureAwait(false);

			lock (_runningLock)
			{
				_isRunning = false;
			}

			await RunAsync().ConfigureAwait(false);
		}
	}
}

