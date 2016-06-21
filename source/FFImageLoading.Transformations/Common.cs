using System;
namespace FFImageLoading.Transformations
{
	public static class Common
	{
		public static string DoNotReferenceMessage
		{
			get
			{
				return "You are referencing the Portable version in your App - you need to reference the platform (iOS/Android) version";
			}
		}
	}
}

