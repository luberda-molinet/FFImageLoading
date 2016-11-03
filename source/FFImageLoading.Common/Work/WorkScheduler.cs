using System;
using FFImageLoading.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using FFImageLoading.Config;

namespace FFImageLoading.Work
{
    public class WorkScheduler : IWorkScheduler
    {
        Task _dispatch = Task.FromResult<byte>(1);
        protected readonly object _pendingTasksLock = new object();
        int _currentPosition; // useful?
        int _statsTotalPending;
        int _statsTotalRunning;
        int _statsTotalMemoryCacheHits;
        int _statsTotalWaiting;
        long _loadCount;

        public WorkScheduler(Configuration configuration, IPlatformPerformance performance)
        {
            Configuration = configuration;
            Performance = performance;
            PendingTasks = new List<PendingTask>();
            RunningTasks = new Dictionary<string, PendingTask>();
        }

        protected int MaxParallelTasks 
        { 
            get
            {
                if (Configuration.SchedulerMaxParallelTasksFactory != null)
                    return Configuration.SchedulerMaxParallelTasksFactory(Configuration);

                return Configuration.SchedulerMaxParallelTasks;
            }
        }

        protected IPlatformPerformance Performance { get; private set; }
        protected List<PendingTask> PendingTasks { get; private set; }
        protected Dictionary<string, PendingTask> RunningTasks { get; private set; }

        protected Configuration Configuration { get; private set; }
        protected IMiniLogger Logger { get { return Configuration.Logger; } }

        protected virtual void EvictStaleTasks()
        {
            lock (_pendingTasksLock)
            {
                var toRemove = PendingTasks.Where(v => v.FrameworkWrappingTask == null || v.ImageLoadingTask == null
                                   || v.ImageLoadingTask.IsCancelled || v.ImageLoadingTask.IsCompleted)
                            .ToList();

                foreach (var task in toRemove)
                {
                    task?.ImageLoadingTask?.Dispose();
                    PendingTasks.Remove(task);
                }
            }
        }

        public virtual void Cancel(IImageLoaderTask task)
        {
            try
            {
                if (task != null && !task.IsCancelled && !task.IsCompleted)
                {
                    task.Cancel();
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("Cancelling task failed: {0}", task?.Key), e);
            }
            finally
            {
                task?.Dispose();
                EvictStaleTasks();
            }
        }

        public virtual void Cancel(Func<IImageLoaderTask, bool> predicate)
        {
            lock (_pendingTasksLock)
            {
                foreach (var task in PendingTasks.Where(p => predicate(p.ImageLoadingTask)).ToList()) // FMT: here we need a copy since cancelling will trigger them to be removed, hence collection is modified during enumeration
                {
                    task.ImageLoadingTask.Cancel();
                }
            }
        }

        public bool ExitTasksEarly { get; private set; }

        public void SetExitTasksEarly(bool exitTasksEarly)
        {
            if (ExitTasksEarly == exitTasksEarly)
                return;

            ExitTasksEarly = exitTasksEarly;

            if (exitTasksEarly)
            {
                Logger.Debug("ExitTasksEarly enabled.");

                lock (_pendingTasksLock)
                {
                    foreach (var task in PendingTasks.ToList()) // FMT: here we need a copy since cancelling will trigger them to be removed, hence collection is modified during enumeration
                        task.ImageLoadingTask.Cancel();

                    PendingTasks.Clear();
                }
            }
            else
            {
                Logger.Debug("ExitTasksEarly disabled.");
            }
        }

        public bool PauseWork { get; private set; }

        public void SetPauseWork(bool pauseWork)
        {
            if (PauseWork == pauseWork)
                return;

            PauseWork = pauseWork;

            if (pauseWork)
            {
                Logger.Debug("SetPauseWork enabled.");
            }
            else
            {
                Logger.Debug("SetPauseWork disabled.");
                TakeFromPendingTasksAndRun();
            }
        }

        public virtual void RemovePendingTask(IImageLoaderTask task)
        {
            lock (_pendingTasksLock)
            {
                PendingTasks.RemoveAll(p => p.ImageLoadingTask == task);
            }
        }

		public virtual async void LoadImage(IImageLoaderTask task)
		{
			Interlocked.Increment(ref _loadCount);

            EvictStaleTasks();

            if (Configuration.VerbosePerformanceLogging && (_loadCount % 10) == 0)
			{
				LogSchedulerStats();
			}

            if (task?.Parameters?.Source != ImageSource.Stream && string.IsNullOrWhiteSpace(task?.Parameters?.Path))
            {
                Logger.Debug("ImageService: null path ignored");
                task?.Dispose();
                return;
            }

			if (task == null)
				return;

            if (task.IsCancelled || task.IsCompleted)
			{
				task?.Dispose();
				return;
			}

			if (task.Parameters.DelayInMs != null && task.Parameters.DelayInMs > 0)
			{
				await Task.Delay(task.Parameters.DelayInMs.Value).ConfigureAwait(false);
			}

			// If we have the image in memory then it's pointless to schedule the job: just display it straight away
			if (task.CanUseMemoryCache)
			{
                if (await task.TryLoadFromMemoryCacheAsync().ConfigureAwait(false))
                {
                    Interlocked.Increment(ref _statsTotalMemoryCacheHits);
                    task?.Dispose();
                    return;
                }
			}

            _dispatch = _dispatch.ContinueWith(t =>
            {
                try
                {
                    EvictStaleTasks();

                    if (task.IsCancelled || task.IsCompleted)
                    {
                        task?.Dispose();
                        return;
                    }

                    if (!task.Parameters.Preload)
                    {
                        lock (_pendingTasksLock)
                        {
                            foreach (var pendingTask in PendingTasks.ToList()) // FMT: here we need a copy since cancelling will trigger them to be removed, hence collection is modified during enumeration
                            {
                                if (pendingTask.ImageLoadingTask != null && pendingTask.ImageLoadingTask.UsesSameNativeControl(task))
                                    pendingTask.ImageLoadingTask.CancelIfNeeded();
                            }
                        }
                    }

                    if (task.IsCancelled || ExitTasksEarly)
                    {
                        task?.Dispose();
                        return;
                    }

                    QueueImageLoadingTask(task);
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Image loaded failed: {0}", task?.Key), ex);
                }
            });
        }

        protected void QueueImageLoadingTask(IImageLoaderTask task)
        {
            int position = Interlocked.Increment(ref _currentPosition);
            var currentPendingTask = new PendingTask() { Position = position, ImageLoadingTask = task, FrameworkWrappingTask = CreateFrameworkTask(task) };

            PendingTask similarRunningTask = null;
            lock (_pendingTasksLock)
            {
                similarRunningTask = PendingTasks.FirstOrDefault(t => t.ImageLoadingTask.Key == task.Key);
                if (similarRunningTask == null)
                {
                    Interlocked.Increment(ref _statsTotalPending);
                    PendingTasks.Add(currentPendingTask);
                }
                else
                {
                    similarRunningTask.Position = position;
                }
            }

            if (PauseWork)
                return;

            if (similarRunningTask == null || !currentPendingTask.ImageLoadingTask.CanUseMemoryCache)
            {
                TakeFromPendingTasksAndRun();
            }
            else
            {
                WaitForSimilarTaskFinished(currentPendingTask, similarRunningTask);
            }
        }

        protected async void WaitForSimilarTaskFinished(PendingTask currentPendingTask, PendingTask taskForSimilarKey)
        {
            Interlocked.Increment(ref _statsTotalWaiting);

            if (taskForSimilarKey.FrameworkWrappingTask == null)
            {
                lock (_pendingTasksLock)
                {
                    Interlocked.Increment(ref _statsTotalPending);
                    PendingTasks.Add(currentPendingTask);
                }

                TakeFromPendingTasksAndRun();
                return;
            }

            Logger.Debug(string.Format("Wait for similar request for key: {0}", taskForSimilarKey.ImageLoadingTask.Key));
            await taskForSimilarKey.FrameworkWrappingTask.ConfigureAwait(false);

            // Now our image should be in the cache
            var cacheFound = await currentPendingTask.ImageLoadingTask.TryLoadFromMemoryCacheAsync().ConfigureAwait(false);
            if (!cacheFound)
            {
                lock (_pendingTasksLock)
                {
                    Interlocked.Increment(ref _statsTotalPending);
                    PendingTasks.Add(currentPendingTask);
                }

                TakeFromPendingTasksAndRun();
                return;
            }
            else
            {
                currentPendingTask?.ImageLoadingTask?.Dispose();
            }
        }

        protected async void TakeFromPendingTasksAndRun()
        {
            await TakeFromPendingTasksAndRunAsync().ConfigureAwait(false); // FMT: we limit concurrent work using MaxParallelTasks
        }

        protected Task CreateFrameworkTask(IImageLoaderTask imageLoadingTask)
        {
            var parameters = imageLoadingTask.Parameters;

            var tcs = new TaskCompletionSource<bool>();

            var successCallback = parameters.OnSuccess;
            parameters.Success((size, result) =>
            {
                tcs.TrySetResult(true);
                successCallback?.Invoke(size, result);
            });

            var finishCallback = parameters.OnFinish;
            parameters.Finish(sw =>
            {
                tcs.TrySetResult(false);
                finishCallback?.Invoke(sw);
            });

            return tcs.Task;
        }

        protected int GetDefaultPriority(ImageSource source)
        {
            if (source == ImageSource.ApplicationBundle || source == ImageSource.CompiledResource)
                return (int)LoadingPriority.Normal + 2;
            
            if (source == ImageSource.Filepath)
                return (int)LoadingPriority.Normal + 1;

            return (int)LoadingPriority.Normal;
        }

        protected async Task TakeFromPendingTasksAndRunAsync()
        {
            Dictionary<string, PendingTask> tasksToRun = null;

            lock (_pendingTasksLock)
            {
                if (RunningTasks.Count >= MaxParallelTasks)
                    return;

                int numberOfTasks = MaxParallelTasks - RunningTasks.Count;
                tasksToRun = new Dictionary<string, PendingTask>();

                foreach (var task in PendingTasks.Where(t => !t.ImageLoadingTask.IsCancelled && !t.ImageLoadingTask.IsCompleted)
                    .OrderByDescending(t => t.ImageLoadingTask.Parameters.Priority ?? GetDefaultPriority(t.ImageLoadingTask.Parameters.Source))
                    .ThenBy(t => t.Position))
                {
                    // We don't want to load, at the same time, images that have same key or same raw key at the same time
                    // This way we prevent concurrent downloads and benefit from caches

                    string rawKey = task.ImageLoadingTask.KeyRaw;
                    if (RunningTasks.ContainsKey(rawKey) || tasksToRun.ContainsKey(rawKey))
                        continue;

                    tasksToRun.Add(rawKey, task);

                    if (tasksToRun.Count == numberOfTasks)
                        break;
                }
            }

            if (tasksToRun != null && tasksToRun.Count > 0)
            {
                if (tasksToRun.Count == 1)
                {
                    await RunImageLoadingTaskAsync(tasksToRun.Values.First(), false).ConfigureAwait(false);
                }
                else
                {
                    var tasks = tasksToRun.Select(p => RunImageLoadingTaskAsync(p.Value, true));
                    await Task.WhenAny(tasks).ConfigureAwait(false);
                }   
            }
        }

        protected async Task RunImageLoadingTaskAsync(PendingTask pendingTask, bool scheduleOnThreadPool)
        {
            var key = pendingTask.ImageLoadingTask.Key;

            lock (_pendingTasksLock)
            {
                if (RunningTasks.ContainsKey(key))
                    return;
                
                RunningTasks.Add(key, pendingTask);
                Interlocked.Increment(ref _statsTotalRunning);
            }

            try
            {
                if (Configuration.VerbosePerformanceLogging)
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
                    Logger.Debug(string.Format("[PERFORMANCE] RunAsync - NetManagedThreadId: {0}, NativeThreadId: {1}, Execution: {2} ms, ThreadPool: {3}, Key: {4}",
                                                Performance.GetCurrentManagedThreadId(),
                                                Performance.GetCurrentSystemThreadId(),
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
                    RunningTasks.Remove(key);
                }
                pendingTask?.ImageLoadingTask?.Dispose();

                await TakeFromPendingTasksAndRunAsync().ConfigureAwait(false);
            }
        }

        protected void LogSchedulerStats()
        {
            Logger.Debug(string.Format("[PERFORMANCE] Scheduler - Max: {0}, Pending: {1}, Running: {2}, TotalPending: {3}, TotalRunning: {4}, TotalMemoryCacheHit: {5}, TotalWaiting: {6}",
                        MaxParallelTasks,
                        PendingTasks.Count,
                        RunningTasks.Count,
                        _statsTotalPending,
                        _statsTotalRunning,
                        _statsTotalMemoryCacheHits,
                        _statsTotalWaiting));
            
            Logger.Debug(Performance.GetMemoryInfo());
        }

        protected class PendingTask
        {
            public int Position { get; set; }

            public IImageLoaderTask ImageLoadingTask { get; set; }

            public Task FrameworkWrappingTask { get; set; }
        }
    }
}
