using System;

namespace FFImageLoading.Transformations
{
	internal static class Helpers
	{
		private const string _doNotReferenceMessage = "You are referencing Mock transformations implementation. Please reference platform specific package";

		public static void ThrowOrDefault()
		{
			if (ImageService.EnableMockImageService)
				return;

			throw new Exception(_doNotReferenceMessage);
		}

		public static T ThrowOrDefault<T>()
		{
			if (ImageService.EnableMockImageService)
				return default;

			throw new Exception(_doNotReferenceMessage);
		}
	}
}

