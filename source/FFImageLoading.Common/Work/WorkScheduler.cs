﻿using System;
using FFImageLoading.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using FFImageLoading.Config;
using FFImageLoading.Concurrency;

namespace FFImageLoading.Work
{
    public class WorkScheduler : IWorkScheduler
    {
        readonly object _runningTasksLock = new object();
        readonly object _similarTasksLock = new object();

        int _statsTotalPending;
        int _statsTotalRunning;
        int _statsTotalMemoryCacheHits;
        int _statsTotalWaiting;
        long _loadCount;

        public WorkScheduler(Configuration configuration, IPlatformPerformance performance)
        {
            Configuration = configuration;
            Performance = performance;
            PendingTasks = new PendingTasksQueue();
            RunningTasks = new Dictionary<string, IImageLoaderTask>();
            SimilarTasks = new List<IImageLoaderTask>();
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
        protected PendingTasksQueue PendingTasks { get; private set; }
        protected Dictionary<string, IImageLoaderTask> RunningTasks { get; private set; }
        protected List<IImageLoaderTask> SimilarTasks { get; private set; }
        protected Configuration Configuration { get; private set; }
        protected IMiniLogger Logger { get { return Configuration.Logger; } }

        public virtual void Cancel(Func<IImageLoaderTask, bool> predicate)
        {
            lock (_similarTasksLock)
            {
                foreach (var task in PendingTasks.Where(p => predicate(p)))
                {
                    task?.Cancel();
                }

                var items = SimilarTasks.Where(v => predicate(v)).ToList();
                foreach (var item in items)
                {
                    SimilarTasks.Remove(item);
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

                lock (_similarTasksLock)
                {
                    foreach (var task in PendingTasks)
                        task?.Cancel();

                    PendingTasks.Clear();

                    foreach (var task in SimilarTasks)
                        task?.Cancel();

                    SimilarTasks.Clear();
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
            if (task != null)
            {
                PendingTasks.Remove(task);
                SimilarTasks.Remove(task);
            }
        }

		public virtual async void LoadImage(IImageLoaderTask task)
		{
            try
            {
                Interlocked.Increment(ref _loadCount);

                if (task == null)
                    return;

                if (task.IsCancelled || task.IsCompleted || ExitTasksEarly)
                {
                    if (!task.IsCompleted)
                        task?.Dispose();
                    return;
                }

                if (Configuration.VerbosePerformanceLogging && (_loadCount % 10) == 0)
                {

    				LogSchedulerStats();
                }

                if (task?.Parameters?.Source != ImageSource.Stream && string.IsNullOrWhiteSpace(task?.Parameters?.Path))
                {
                    Logger.Error("ImageService: null path ignored");
                    task?.Dispose();
                    return;
                }

                if (task.Parameters.DelayInMs != null && task.Parameters.DelayInMs > 0)
                {
                    await Task.Delay(task.Parameters.DelayInMs.Value).ConfigureAwait(false);
                }

                await task.Init();

                // If we have the image in memory then it's pointless to schedule the job: just display it straight away
                if (task.CanUseMemoryCache && await task.TryLoadFromMemoryCacheAsync().ConfigureAwait(false))
                {
                    Interlocked.Increment(ref _statsTotalMemoryCacheHits);
                    task?.Dispose();
                    return;
                }

                QueueImageLoadingTask(task);
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Image loaded failed: {0}", task?.Key), ex);
            }
        }

        void Enqueue(IImageLoaderTask task)
        {
            PendingTasks.Enqueue(task, task.Parameters.Priority ?? GetDefaultPriority(task.Parameters.Source));
        }

        protected void QueueImageLoadingTask(IImageLoaderTask task)
        {
            if (task.IsCancelled || task.IsCompleted || ExitTasksEarly)
            {
                if (!task.IsCompleted)
                    task?.Dispose();
                
                return;
            }

            IImageLoaderTask similarRunningTask = null;
            if (!task.Parameters.Preload)
            {
                PendingTasks.CancelWhenUsesSameNativeControl(task);
            }

            similarRunningTask = PendingTasks.FirstOrDefaultByRawKey(task.KeyRaw);
            if (similarRunningTask == null)
            {
                Interlocked.Increment(ref _statsTotalPending);
                Enqueue(task);
            }
            else
            {
                if (task.Parameters.Priority.HasValue && (!similarRunningTask.Parameters.Priority.HasValue
                    || task.Parameters.Priority.Value > similarRunningTask.Parameters.Priority.Value))
                {
                    similarRunningTask.Parameters.WithPriority(task.Parameters.Priority.Value);
                    PendingTasks.UpdatePriority(similarRunningTask, task.Parameters.Priority.Value);
                }

                if (task.Parameters.OnDownloadProgress != null)
                {
                    var similarTaskOnDownloadProgress = similarRunningTask.Parameters.OnDownloadProgress;
                    similarRunningTask.Parameters.DownloadProgress((DownloadProgress obj) =>
                    {
                        similarTaskOnDownloadProgress?.Invoke(obj);
                        task.Parameters.OnDownloadProgress(obj);
                    });
                }
            }

            if (PauseWork)
                return;

            if (similarRunningTask == null || !task.CanUseMemoryCache)
            {
                TakeFromPendingTasksAndRun();
            }
            else
            {
                Interlocked.Increment(ref _statsTotalWaiting);
                Logger.Debug(string.Format("Wait for similar request for key: {0}", task.Key));
                lock (_similarTasksLock)
                {
                    SimilarTasks.Add(task);
                }
            }
        }

        protected void TakeFromPendingTasksAndRun()
        {
            Task.Factory.StartNew(async () =>
            {
                await TakeFromPendingTasksAndRunAsync().ConfigureAwait(false); // FMT: we limit concurrent work using MaxParallelTasks
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler, TaskScheduler.Default).ConfigureAwait(false);
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

        protected async Task TakeFromPendingTasksAndRunAsync()
        {
            if (PendingTasks.Count == 0)
                return;

            Dictionary<string, IImageLoaderTask> tasksToRun = null;

            int preloadOrUrlTasksCount = 0;
            int urlTasksCount = 0;
            int preloadTasksCount = 0;

            lock (_runningTasksLock)
            {
                if (RunningTasks.Count >= MaxParallelTasks)
                {
                    urlTasksCount = RunningTasks.Count(v => v.Value != null && (!v.Value.Parameters.Preload && v.Value.Parameters.Source == ImageSource.Url));
                    preloadTasksCount = RunningTasks.Count(v => v.Value != null && v.Value.Parameters.Preload);
                    preloadOrUrlTasksCount = preloadTasksCount + urlTasksCount;

                    if (preloadOrUrlTasksCount == 0 || preloadOrUrlTasksCount != MaxParallelTasks)
                        return;

                    // Allow only half of MaxParallelTasks as additional allowed tasks when preloading occurs to prevent starvation
                    if (RunningTasks.Count - Math.Max(1, Math.Min(preloadOrUrlTasksCount, MaxParallelTasks / 2)) >= MaxParallelTasks)
                        return;
                }

                int numberOfTasks = MaxParallelTasks - RunningTasks.Count + Math.Min(preloadOrUrlTasksCount, MaxParallelTasks / 2);
                tasksToRun = new Dictionary<string, IImageLoaderTask>();

                while (tasksToRun.Count < numberOfTasks && PendingTasks.Count > 0)
                {
                    var task = PendingTasks.Dequeue();

                    if (task == null || task.IsCancelled || task.IsCompleted)
                        continue;

                    // We don't want to load, at the same time, images that have same key or same raw key at the same time
                    // This way we prevent concurrent downloads and benefit from caches
                    string rawKey = task.KeyRaw;
                    if (RunningTasks.ContainsKey(rawKey) || tasksToRun.ContainsKey(rawKey))
                    {
                        lock (_similarTasksLock)
                        {
                            SimilarTasks.Add(task);
                            continue;
                        }
                    }

                    if (preloadOrUrlTasksCount != 0)
                    {
                        if (!task.Parameters.Preload && (urlTasksCount == 0 || task.Parameters.Source != ImageSource.Url))
                            tasksToRun.Add(rawKey, task);
                        else
                        {
                            Enqueue(task);
                            break;
                        }
                    }
                    else
                    {
                        tasksToRun.Add(rawKey, task);
                    }
                }

                foreach (var item in tasksToRun)
                {
                    RunningTasks.Add(item.Key, item.Value);
                    Interlocked.Increment(ref _statsTotalRunning);
                }
            }

            if (tasksToRun != null && tasksToRun.Count > 0)
            {
                var tasks = tasksToRun.Select(p => RunImageLoadingTaskAsync(p.Value));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        protected async Task RunImageLoadingTaskAsync(IImageLoaderTask pendingTask)
        {
            var keyRaw = pendingTask.KeyRaw;

            try
            {
                if (Configuration.VerbosePerformanceLogging)
                {
                    LogSchedulerStats();
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    await pendingTask.RunAsync().ConfigureAwait(false);

                    stopwatch.Stop();

                    Logger.Debug(string.Format("[PERFORMANCE] RunAsync - NetManagedThreadId: {0}, NativeThreadId: {1}, Execution: {2} ms, Key: {3}",
                                                Performance.GetCurrentManagedThreadId(),
                                                Performance.GetCurrentSystemThreadId(),
                                                stopwatch.Elapsed.Milliseconds,
                                                pendingTask.Key));
                }
                else
                {
                    await pendingTask.RunAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                lock (_runningTasksLock)
                {
                    RunningTasks.Remove(keyRaw);

                    lock (_similarTasksLock)
                    {
                        if (SimilarTasks.Count > 0)
                        {
                            SimilarTasks.RemoveAll(v => v == null || v.IsCompleted || v.IsCancelled);
                            var similarItems = SimilarTasks.Where(v => v.KeyRaw == keyRaw).ToList();
                            foreach (var similar in similarItems)
                            {
                                SimilarTasks.Remove(similar);
                                LoadImage(similar);
                            }
                        }
                    }
                }

                pendingTask?.Dispose();
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

        protected virtual int GetDefaultPriority(ImageSource source)
        {
            if (source == ImageSource.ApplicationBundle || source == ImageSource.CompiledResource)
                return (int)LoadingPriority.Normal + 2;

            if (source == ImageSource.Filepath)
                return (int)LoadingPriority.Normal + 1;

            return (int)LoadingPriority.Normal;
        }
    }
}
