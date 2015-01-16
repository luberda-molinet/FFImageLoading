using System;
using HGR.Mobile.Droid.ImageLoading.Views;
using System.Threading.Tasks;

namespace HGR.Mobile.Droid.ImageLoading.Work
{
    public static class TaskParameterExtensions
    {
        /// <summary>
        /// Loads the image into given imageView using defined parameters.
        /// </summary>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="imageView">Image view that should receive the image.</param>
        public static void Into(this TaskParameter parameters, ImageViewAsync imageView)
        {
            var task = new ImageLoaderTask(parameters, imageView);
            ImageService.LoadImage(task.Key, task, imageView);
        }

        /// <summary>
        /// Only use this method if you plan to handle exceptions in your code. Awaiting this method will give you this flexibility.
        /// </summary>
        /// <returns>An awaitable Task.</returns>
        /// <param name="parameters">Parameters for loading the image.</param>
        /// <param name="imageView">Image view that should receive the image.</param>
        public static Task IntoAsync(this TaskParameter parameters, ImageViewAsync imageView)
        {
            var userErrorCallback = parameters.OnError;
            var finishCallback = parameters.OnFinish;
            var tcs = new TaskCompletionSource<object>();

            parameters
                .Error(ex => {
                    userErrorCallback(ex);
                    tcs.SetException(ex);
                })
                .Finish(() => {
                    finishCallback();
                    tcs.TrySetResult(string.Empty); // we should use TrySetResult since SetException could have been called earlier. It is not allowed to set result after SetException
                })
                .Into(imageView);

            return tcs.Task;
        }
    }
}

