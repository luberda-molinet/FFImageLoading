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

        int? FadeAnimationDuration { get; set; }

        bool? FadeAnimationForCachedImages { get; set; }

        bool? TransformPlaceholders { get; set; }

        CacheType? CacheType { get; set; }

        List<ITransformation> Transformations { get; set; }

        bool? InvalidateLayoutAfterLoaded { get; set; }

        IDataResolver CustomDataResolver { get; set; }

        IDataResolver CustomLoadingPlaceholderDataResolver { get; set; }

        IDataResolver CustomErrorPlaceholderDataResolver { get; set; }

        event EventHandler<Args.SuccessEventArgs> OnSuccess;

        event EventHandler<Args.ErrorEventArgs> OnError;

        event EventHandler<Args.FinishEventArgs> OnFinish;

        event EventHandler<Args.DownloadStartedEventArgs> OnDownloadStarted;

        event EventHandler<Args.DownloadProgressEventArgs> OnDownloadProgress;

        event EventHandler<Args.FileWriteFinishedEventArgs> OnFileWriteFinished;
    }
}
