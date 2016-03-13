using System;

namespace FFImageLoading
{
	public class ImageService: IImageService
	{
		private const string DoNotReference = "You are referencing the Portable version in your App - you need to reference the platform (iOS/Android) version";

		public static IImageService Instance
		{
			get
			{
				throw new Exception(DoNotReference);
			}
		}

		private ImageService() { }

		#region IImageService implementation

		public FFImageLoading.Config.Configuration Config
		{
			get
			{
				throw new Exception(DoNotReference);
			}
		}

		public void Initialize(FFImageLoading.Config.Configuration configuration)
		{
			throw new Exception(DoNotReference);
		}

		public FFImageLoading.Work.TaskParameter LoadFile(string filepath)
		{
			throw new Exception(DoNotReference);
		}

		public FFImageLoading.Work.TaskParameter LoadUrl(string url, TimeSpan? cacheDuration = null)
		{
			throw new Exception(DoNotReference);
		}

		public FFImageLoading.Work.TaskParameter LoadFileFromApplicationBundle(string filepath)
		{
			throw new Exception(DoNotReference);
		}

		public FFImageLoading.Work.TaskParameter LoadCompiledResource(string resourceName)
		{
			throw new Exception(DoNotReference);
		}

		public FFImageLoading.Work.TaskParameter LoadStream(Func<System.Threading.CancellationToken, System.Threading.Tasks.Task<System.IO.Stream>> stream)
		{
			throw new Exception(DoNotReference);
		}

		public void SetExitTasksEarly(bool exitTasksEarly)
		{
			throw new Exception(DoNotReference);
		}

		public void SetPauseWork(bool pauseWork)
		{
			throw new Exception(DoNotReference);
		}

		public void CancelWorkFor(FFImageLoading.Work.IImageLoaderTask task)
		{
			throw new Exception(DoNotReference);
		}

		public void RemovePendingTask(FFImageLoading.Work.IImageLoaderTask task)
		{
			throw new Exception(DoNotReference);
		}

		public void LoadImage(FFImageLoading.Work.IImageLoaderTask task)
		{
			throw new Exception(DoNotReference);
		}

		System.Threading.Tasks.Task IImageService.InvalidateCacheAsync(FFImageLoading.Cache.CacheType cacheType)
		{
			throw new NotImplementedException();
		}

		public void InvalidateMemoryCache()
		{
			throw new Exception(DoNotReference);
		}

		public System.Threading.Tasks.Task InvalidateDiskCacheAsync()
		{
			throw new Exception(DoNotReference);
		}

		public System.Threading.Tasks.Task InvalidateCacheEntryAsync(string key, FFImageLoading.Cache.CacheType cacheType, bool removeSimilar = false)
		{
			throw new Exception(DoNotReference);
		}

		public System.Threading.Tasks.Task<bool> DownloadImageAndAddToDiskCacheAsync(string imageUrl, System.Threading.CancellationToken cancellationToken, TimeSpan? duration = null, string customCacheKey = null)
		{
			throw new Exception(DoNotReference);
		}

		public bool ExitTasksEarly
		{
			get
			{
				throw new Exception(DoNotReference);
			}
		}

		#endregion
	}
}

