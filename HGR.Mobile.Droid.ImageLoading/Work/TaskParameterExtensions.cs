using System;
using HGR.Mobile.Droid.ImageLoading.Views;

namespace HGR.Mobile.Droid.ImageLoading.Work
{
    public static class TaskParameterExtensions
    {
        public static void Into(this TaskParameter parameters, ImageViewAsync imageView)
        {
            var task = new ImageLoaderTask(parameters, imageView);
            ImageService.LoadImage(task.Key, task, imageView);
        }
    }
}

