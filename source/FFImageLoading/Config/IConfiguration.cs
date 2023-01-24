using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using FFImageLoading.Work;

namespace FFImageLoading.Config
{
	public interface IConfiguration
	{
		bool AllowUpscale { get; set; }
		bool AnimateGifs { get; set; }
		bool BitmapOptimizations { get; set; }
		bool ClearMemoryCacheOnOutOfMemory { get; set; }
		int DecodingMaxParallelTasks { get; set; }
		int DelayInMs { get; set; }
		TimeSpan DiskCacheDuration { get; set; }
		string DiskCachePath { get; set; }
		InterpolationMode DownsampleInterpolationMode { get; set; }
		bool ExecuteCallbacksOnUIThread { get; set; }
		int FadeAnimationDuration { get; set; }
		bool FadeAnimationEnabled { get; set; }
		bool FadeAnimationForCachedImages { get; set; }
		HttpClient HttpClient { get; set; }
		int HttpHeadersTimeout { get; set; }
		int HttpReadBufferSize { get; set; }
		int HttpReadTimeout { get; set; }
		bool InvalidateLayout { get; set; }
		int MaxMemoryCacheSize { get; set; }
		int SchedulerMaxParallelTasks { get; set; }
		Func<IConfiguration, int> SchedulerMaxParallelTasksFactory { get; set; }
		bool StreamChecksumsAsKeys { get; set; }
		bool TransformPlaceholders { get; set; }
		bool TryToReadDiskCacheDurationFromHttpHeaders { get; set; }
		bool VerboseLoadingCancelledLogging { get; set; }
		bool VerboseLogging { get; set; }
		bool VerboseMemoryCacheLogging { get; set; }
		bool VerbosePerformanceLogging { get; set; }
	}
}
