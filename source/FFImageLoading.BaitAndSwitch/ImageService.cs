using System;

namespace FFImageLoading
{
    /// <summary>
    /// ImageService
    /// </summary>
	public class ImageService
	{
		private const string DoNotReference = "You are referencing the Portable version in your App - you need to reference the platform (iOS/Android) version";

        /// <summary>
        /// Instance
        /// </summary>
		public static IImageService Instance
		{
			get
			{
				throw new Exception(DoNotReference);
			}
		}
	}
}

