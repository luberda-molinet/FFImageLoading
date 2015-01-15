using Android.Widget;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using System;

namespace HGR.Mobile.Droid.ImageLoading.Work
{
    public static class ImageService
    {
        private static readonly ImageWorker _worker;

        static ImageService()
        {
            _worker = new ImageWorker();
        }

        public static bool ExitTasksEarly
        {
            get
            {
                return _worker.ExitTasksEarly;
            }
        }

        public static void SetExitTasksEarly(bool exitTasksEarly)
        {
            _worker.SetExitTasksEarly(exitTasksEarly);
        }

        public static void SetPauseWork(bool pauseWork)
        {
            _worker.SetPauseWork(pauseWork);
        }

        public static void CancelWorkFor(ImageView imageView)
        {
            _worker.Cancel(imageView);
        }

        public static void RemovePendingTask(ImageLoaderTask task)
        {
            _worker.RemovePendingTask(task);
        }

        public static void LoadImage(string key, ImageLoaderTask task, ImageView imageView, Action action)
        {
            _worker.LoadImage(key, task, imageView, action);
        }
    }
}