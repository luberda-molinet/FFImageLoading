using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace FFImageLoading.Forms.Sample.Droid
{
	[Activity(Label = "FFImageLoading.Forms.Sample.Droid", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
	{
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			FFImageLoading.Forms.Droid.CachedImageRenderer.Init();

			var config = new FFImageLoading.Config.Configuration()
			{
				VerboseLogging = false,
				VerbosePerformanceLogging = false,
				VerboseMemoryCacheLogging = false,
				VerboseLoadingCancelledLogging = false,
				Logger = new CustomLogger(),
			};
			ImageService.Instance.Initialize(config);

			global::Xamarin.Forms.Forms.Init(this, bundle);
			LoadApplication(new App());
		}

		public class CustomLogger : FFImageLoading.Helpers.IMiniLogger
		{
			public void Debug(string message)
			{
				Console.WriteLine(message);
			}

			public void Error(string errorMessage)
			{
				Console.WriteLine(errorMessage);
			}

			public void Error(string errorMessage, Exception ex)
			{
				Error(errorMessage + System.Environment.NewLine + ex.ToString());
			}
		}
	}
}

