using System;
using System.Drawing;
using HGR.Mobile.Droid.ImageLoading.Views;

namespace HGR.Mobile.Droid.ImageLoading.Work
{
    internal enum ImageSource
    {
        Filepath,
        Url
    }

    public class TaskParameter
    {
        public static TaskParameter FromFile(string filepath)
        {
            return new TaskParameter() { Source = ImageSource.Filepath, Path = filepath };
        }

        public static TaskParameter FromUrl(string url, TimeSpan? cacheDuration = null)
        {
            return new TaskParameter() { Source = ImageSource.Url, Path = url, CacheDuration = cacheDuration };
        }

        private TaskParameter()
        {
            // default values so we don't have a null value
            OnSuccess = () => {
            };

            OnError = ex => {
            };
        }

        internal ImageSource Source { get; private set; }

        internal string Path { get; private set; }

        internal TimeSpan? CacheDuration { get; private set; }

        internal SizeF DownSampleSize { get; private set; }

        internal int RetryCount { get; private set; }

        internal int RetryDelayInMs { get; private set; }

        internal Action OnSuccess { get; private set; }

        internal Action<Exception> OnError { get; private set; }

        public TaskParameter DownSample(int width = 0, int height = 0)
        {
            DownSampleSize = new SizeF(width, height);
            return this;
        }

        public TaskParameter Retry(int retryCount = 0, int millisecondDelay = 0)
        {
            RetryCount = retryCount;
            RetryDelayInMs = millisecondDelay;
            return this;
        }

        public TaskParameter Success(Action action)
        {
            if (action == null)
                throw new Exception("Given lambda should not be null.");

            OnSuccess = action;
            return this;
        }

        public TaskParameter Error(Action<Exception> action)
        {
            if (action == null)
                throw new Exception("Given lambda should not be null.");

            OnError = action;
            return this;
        }
    }
}

