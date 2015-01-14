using Android.Widget;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;

namespace HGR.Mobile.Droid.ImageLoading.Work
{
	public interface IImageWorker : IImageWorkerBase<string>
	{
	}

	public class ImageWorker : ImageWorkerBase<string>, IImageWorker
	{
        public override ImageLoaderTask GetLoaderTask(string path, ImageView imageView)
		{
            return new ImageLoaderTask(path, imageView);
		}
	}

	public static class ImageWorkerService
	{
		public static bool ExitTasksEarly = false;
        internal static bool PauseWork;
        internal static object PauseWorkLock = new object();
        internal static List<ImageLoaderTask> PendingTasks = new List<ImageLoaderTask>();
	}
}