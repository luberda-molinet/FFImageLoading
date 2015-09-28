using System;

using Xamarin.Forms;
using FFImageLoading.Forms.Sample.ViewModels;
using DLToolkit.PageFactory;

namespace FFImageLoading.Forms.Sample.Pages
{
	public class SimpleExamplePage : PFContentPage<SimpleExampleViewModel>
	{
		public SimpleExamplePage()
		{
			Title = "Simple Demo";

			var cachedImage = new CachedImage() {
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				WidthRequest = 300,
				HeightRequest = 300,
				CacheDuration = TimeSpan.FromDays(30),
				DownsampleHeight = 300,
				RetryCount = 3,
				RetryDelay = 250,
				TransparencyEnabled = false,
				Source = "http://lorempixel.com/output/city-q-c-600-600-5.jpg",
			};

			Content =  cachedImage;
		}
	}
}


