using System;

namespace FFImageLoading.Work
{
	public class ImageInformation
	{
		public int CurrentWidth { get; private set; }

		public int CurrentHeight { get; private set; }

		public int OriginalWidth { get; private set; }

		public int OriginalHeight { get; private set; }

		public string FilePath { get; private set; }

		public string CacheKey { get; private set; }
	}
}

