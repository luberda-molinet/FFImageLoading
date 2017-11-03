using System;
using System.Drawing;
using System.Collections.Generic;
using FFImageLoading.Work;

#if __IOS__
using Foundation;
using UIKit;
using CoreGraphics;
#elif __ANDROID__
using Android.Util;
using Android.Runtime;
using Android.Content;
using FFImageLoading.Views;
#endif

namespace FFImageLoading.Cross
{
    #if __IOS__
    [Register("MvxImageLoadingView")]
    #elif __ANDROID__
    [Register("ffimageloading.cross.MvxImageLoadingView")]
    #endif
    /// OBSOLETE !!!!!!!!!!!!!!!!!!!!!
    /// OBSOLETE !!!!!!!!!!!!!!!!!!!!!
    /// OBSOLETE !!!!!!!!!!!!!!!!!!!!!
    /// OBSOLETE !!!!!!!!!!!!!!!!!!!!!
    /// OBSOLETE !!!!!!!!!!!!!!!!!!!!!
    /// OBSOLETE !!!!!!!!!!!!!!!!!!!!!
    [Obsolete("Please use a new/better MvxCachedImageView")]
    public class MvxImageLoadingView
    #if __IOS__
        : UIImageView
    #elif __ANDROID__
        : ImageViewAsync
    #endif
    {
        #if __IOS__
        public MvxImageLoadingView() { Initialize(); }
        public MvxImageLoadingView(IntPtr handle) : base(handle) { Initialize(); }
        public MvxImageLoadingView(CGRect frame) : base(frame) { Initialize(); }
        #elif __ANDROID__
        public MvxImageLoadingView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { Initialize(); }
        public MvxImageLoadingView(Context context) : base(context) { Initialize(); }
        public MvxImageLoadingView(Context context, IAttributeSet attrs) : base(context, attrs) { Initialize(); }
        #endif

        private TaskParameter _parameters;
        private string _dataLocation;
        private string _dataLocationUri;

        private void Initialize()
        {
            LoadingPlaceholderSource = ImageSource.Filepath;
            ErrorPlaceholderSource = ImageSource.Filepath;
        }

        public ImageSource? Source { get; set; }

        public TimeSpan? CacheDuration { get; set; }

        public Tuple<int, int> DownSampleSize { get; set; }

        public bool DownSampleUseDipUnits { get; set; }

        public InterpolationMode DownSampleInterpolationMode { get; set; }

        public ImageSource LoadingPlaceholderSource { get; set; }

        public string LoadingPlaceholderPath { get; set; }

        public ImageSource ErrorPlaceholderSource { get; set; }

        public string ErrorPlaceholderPath { get; set; }

        public int RetryCount { get; set; }

        public int RetryDelayInMs { get; set; }

        public Action<ImageInformation, LoadingResult> OnSuccess { get; set; }

        public Action<Exception> OnError { get; set; }

        public Action<IScheduledWork> OnFinish { get; set; }

        public List<ITransformation> Transformations { get; set; }

        [Obsolete]
        public bool? LoadTransparencyChannel { get; set; }

        public bool? BitmapOptimizations { get; set; }

        public bool? FadeAnimationEnabled { get; set; }

        public bool? TransformPlaceholdersEnabled { get; set; }

        public string CustomCacheKey { get; set; }

        public string DataLocation
        {
            get
            {
                return _dataLocation;
            }
            set
            {
                if (Source == null)
                    throw new Exception("ImageSource must be defined prior to define DataLocation.");

                _dataLocation = value;

                CleanParameters();
                _parameters = MakeParams();
                _parameters.Into(this);
            }
        }

        public string DataLocationUri
        {
            get { return _dataLocationUri; }
            set
            {
                _dataLocationUri = value;

                if (string.IsNullOrEmpty(_dataLocationUri))
                    return;

                var dataLocationUriLower = _dataLocationUri.ToLowerInvariant();

                if (dataLocationUriLower.StartsWith("res:"))
                {
                    var resourcePath = _dataLocationUri.Split(new[] { "res:" }, StringSplitOptions.None)[1];
                    Source = ImageSource.CompiledResource;
                    DataLocation = resourcePath;
                }
#if __ANDROID__
                else if (dataLocationUriLower.StartsWith("android.resource"))
                {
                    var substrings = _dataLocationUri.Split(new[] { "/" }, StringSplitOptions.None);
                    var resourceName = Context.Resources.GetResourceEntryName(Convert.ToInt32(substrings[substrings.Length - 1]));
                    Source = ImageSource.CompiledResource;
                    DataLocation = resourceName;
                }
#endif
                else if (dataLocationUriLower.StartsWith("http"))
                {
                    Source = ImageSource.Url;
                    DataLocation = _dataLocationUri;
                }
                else
                {
                    Source = ImageSource.Filepath;
                    DataLocation = _dataLocationUri;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanParameters();
        OnSuccess = null;
        OnError = null;
        OnFinish = null;
      }
            base.Dispose(disposing);
        }

        private void CleanParameters()
        {
            if (_parameters != null)
            {
                _parameters.TryDispose();
                _parameters = null;
            }
        }

        private TaskParameter MakeParams()
        {
            var parameters = InstanciateParams(Source.Value, DataLocation, CacheDuration);

            if (DownSampleSize != null)
            {
                if (DownSampleUseDipUnits)
                {
                    parameters.DownSampleInDip(DownSampleSize.Item1, DownSampleSize.Item2);
                }
                else
                {
                    parameters.DownSample(DownSampleSize.Item1, DownSampleSize.Item2);
                }
            }

            if (LoadingPlaceholderPath != null)
            {
                parameters.LoadingPlaceholder(LoadingPlaceholderPath, LoadingPlaceholderSource);
            }
            if (ErrorPlaceholderPath != null)
            {
                parameters.ErrorPlaceholder(ErrorPlaceholderPath, ErrorPlaceholderSource);
            }

            parameters.DownSampleMode(DownSampleInterpolationMode);
            parameters.Retry(RetryCount, RetryDelayInMs);

            if (OnSuccess != null)
            {
                parameters.Success(OnSuccess);
            }
            if (OnError != null)
            {
                parameters.Error(OnError);
            }
            if (OnFinish != null)
            {
                parameters.Finish(OnFinish);
            }

            if (Transformations != null)
            {
                parameters.Transform(Transformations);
            }

            if (LoadTransparencyChannel.HasValue)
            {
                parameters.TransparencyChannel(LoadTransparencyChannel.Value);
            }

            if (BitmapOptimizations.HasValue)
            {
                parameters.BitmapOptimizations(BitmapOptimizations.Value);
            }

            if (FadeAnimationEnabled.HasValue)
            {
                parameters.FadeAnimation(FadeAnimationEnabled.Value);
            }

            if (TransformPlaceholdersEnabled.HasValue)
            {
                parameters.TransformPlaceholders(TransformPlaceholdersEnabled.Value);
            }

            if (CustomCacheKey != null)
            {
                parameters.CacheKey(CustomCacheKey);
            }

            return parameters;
        }

        private TaskParameter InstanciateParams(ImageSource source, string dataLocation, TimeSpan? cacheDuration)
        {
            switch (source)
            {
                case ImageSource.ApplicationBundle:
                    return TaskParameter.FromApplicationBundle(dataLocation);

                case ImageSource.CompiledResource:
                    return TaskParameter.FromCompiledResource(dataLocation);

                case ImageSource.Filepath:
                    return TaskParameter.FromFile(dataLocation);

                case ImageSource.Stream:
                    throw new Exception("ImageSource Stream is not supported by MvxImageLoadingView");

                case ImageSource.Url:
                    return TaskParameter.FromUrl(dataLocation, cacheDuration);

                default:
                    throw new Exception("Invalid parameters supplied to InstanciateParams");
            }
        }
    }
}

