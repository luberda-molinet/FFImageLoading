using Android.OS;

namespace FFImageLoading.Helpers
{
	internal static class Utils
	{
		public static bool HasFroyo()
		{
			return Build.VERSION.SdkInt >= BuildVersionCodes.Froyo;
		}

		public static bool HasGingerbread()
		{
			return Build.VERSION.SdkInt >= BuildVersionCodes.Gingerbread;
		}

		public static bool HasHoneycomb()
		{
			return Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb;
		}

		public static bool HasHoneycombMr1()
		{
			return Build.VERSION.SdkInt >= BuildVersionCodes.HoneycombMr1;
		}

		public static bool HasJellyBean()
		{
			return Build.VERSION.SdkInt >= BuildVersionCodes.JellyBean;
		}

		public static bool HasKitKat()
		{
			return Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat;
		}
	}
}
