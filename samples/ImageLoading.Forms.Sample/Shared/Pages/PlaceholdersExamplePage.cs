using System;

using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.ViewModels;

namespace FFImageLoading.Forms.Sample.Pages
{
	public class PlaceholdersExamplePage : PFContentPage<PlaceholdersExampleViewModel>
	{
		public PlaceholdersExamplePage()
		{
			Title = "Placeholders Demo";

			var cachedImage = new CachedImage() {
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				WidthRequest = 200,
				HeightRequest = 200,
				DownsampleToViewSize = true,
				CacheDuration = TimeSpan.FromDays(30),
				RetryCount = 0,
				TransparencyEnabled = false,
			};
			cachedImage.SetBinding<PlaceholdersExampleViewModel>(CachedImage.LoadingPlaceholderProperty, v => v.LoadingImagePath);
			cachedImage.SetBinding<PlaceholdersExampleViewModel>(CachedImage.ErrorPlaceholderProperty, v => v.ErrorImagePath);
			cachedImage.SetBinding<PlaceholdersExampleViewModel>(CachedImage.SourceProperty, v => v.ImagePath);

			var button1 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Local Loading Placeholder Example",
				Command = ViewModel.LocalLoadingPlaceholderExampleCommand
			};

			var button2 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Remote Loading Placeholder Example",
				Command = ViewModel.RemoteLoadingPlaceholderExampleCommand
			};

			var button3 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Local Error Placeholder Example",
				Command = ViewModel.LocalErrorPlaceholderExampleCommand
			};

			var button4 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Remote Error Placeholder Example",
				Command = ViewModel.RemoteErrorPlaceholderExampleCommand
			};

			var imagePath = new Label() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				FontSize = 9,
			};
			imagePath.SetBinding<PlaceholdersExampleViewModel>(Label.TextProperty, v => v.ImagePath);

			Content = new ScrollView() {
				Content = new StackLayout { 
					Children = {
						imagePath,
						cachedImage,
						button1, 
						button2, 
						button3, 
						button4,
					}
				}
			};
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			ViewModel.ImagePath = null;
		}
	}
}


