using System;
using System.Collections.Generic;
using FFImageLoading.Cache;
using FFImageLoading.Work;

namespace FFImageLoading.Views
{
    /// <summary>
    /// This interface is used to unify view properties / methods across all platform specific implementations
    /// </summary>
    public interface ICachedImageView
    {
        bool IsLoading { get; }

        int RetryCount { get; set; }

        int RetryDelay { get; set; }

        int LoadingDelay { get; set; }

        double DownsampleWidth { get; set; }

        double DownsampleHeight { get; set; }

        bool DownsampleUseDipUnits { get; set; }

        TimeSpan? CacheDuration { get; set; }

        LoadingPriority LoadingPriority { get; set; }

        bool? BitmapOptimizations { get; set; }

        bool? FadeAnimationEnabled { get; set; }

        bool? TransformPlaceholders { get; set; }

        CacheType? CacheType { get; set; }

        List<ITransformation> Transformations { get; set; }

        IDataResolver CustomDataResolver { get; set; }

        IDataResolver CustomLoadingPlaceholderDataResolver { get; set; }

        IDataResolver CustomErrorPlaceholderDataResolver { get; set; }

        Action<ImageInformation, LoadingResult> OnSuccess { get; set; }

        Action<Exception> OnError { get; set; }

        Action<IScheduledWork> OnFinish { get; set; }

        Action<DownloadInformation> OnDownloadStarted { get; set; }

        Action<FileWriteInfo> OnFileWriteFinished { get; set; }

        Action<DownloadProgress> OnDownloadProgress { get; set; }
    }
}
