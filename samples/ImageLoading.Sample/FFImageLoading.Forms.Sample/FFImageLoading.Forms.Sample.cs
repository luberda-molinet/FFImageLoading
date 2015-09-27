using System;

using Xamarin.Forms;

namespace FFImageLoading.Forms.Sample
{
	public class App : Application
	{
		public App()
		{
			var cachedImage = new CachedImage() {
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				WidthRequest = 300,
				HeightRequest = 300,
				CacheDuration = TimeSpan.FromSeconds(30),
				DownsampleHeight = 300,
				RetryCount = 3,
				RetryDelay = 250,
				TransparencyEnabled = false,
				// Shown after loading error occurs:
				ErrorPlaceholder = "http://lorempixel.com/output/abstract-q-c-300-300-9.jpg",
				// Shown before targe image is loaded:
				LoadingPlaceholder = "icon.png",
				// Target Image:
				Source = "http://lorempixel.com/output/city-q-c-600-600-5.jpg",
			};

			MainPage = new ContentPage() {
				Title = "FFImageLoading.Forms.Sample",
				Content = cachedImage,
			};
		}

		protected override void OnStart()
		{
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}
	}
}

