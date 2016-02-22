using System;

namespace FFImageLoading.Work
{
	public class ImageInformation
	{
		public ImageInformation()
		{
		}

		internal void SetCurrentSize(int width, int height)
		{
			CurrentWidth = width;
			CurrentHeight = height;
		}

		internal void SetOriginalSize(int width, int height)
		{
			OriginalWidth = width;
			OriginalHeight = height;
		}

		internal void SetPath(string path)
		{
			Path = path;
		}

		internal void SetFilePath(string filePath)
		{
			FilePath = filePath;
		}

		internal void SetCacheKey(string cacheKey)
		{
			CacheKey = cacheKey;
		}

		public int CurrentWidth { get; private set; }

		public int CurrentHeight { get; private set; }

		public int OriginalWidth { get; private set; }

		public int OriginalHeight { get; private set; }

		public string Path { get; private set; }

		public string FilePath { get; private set; }

		public string CacheKey { get; private set; }
	}
}

