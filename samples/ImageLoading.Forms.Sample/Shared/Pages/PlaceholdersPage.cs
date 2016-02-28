using System;
using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.PageModels;

namespace FFImageLoading.Forms.Sample.Pages
{
    public class PlaceholdersPage : ContentPage, IBasePage<PlaceholdersPageModel>
	{
		public PlaceholdersPage()
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
            cachedImage.SetBinding<PlaceholdersPageModel>(CachedImage.LoadingPlaceholderProperty, v => v.LoadingImagePath);
            cachedImage.SetBinding<PlaceholdersPageModel>(CachedImage.ErrorPlaceholderProperty, v => v.ErrorImagePath);
            cachedImage.SetBinding<PlaceholdersPageModel>(CachedImage.SourceProperty, v => v.ImagePath);

			var button1 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Local Loading Placeholder Example",
			};
            button1.SetBinding<PlaceholdersPageModel>(Button.CommandProperty, v => v.LocalLoadingCommand);

			var button2 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Remote Loading Placeholder Example",
			};
            button2.SetBinding<PlaceholdersPageModel>(Button.CommandProperty, v => v.RemoteLoadingCommand);

			var button3 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Local Error Placeholder Example",
			};
            button3.SetBinding<PlaceholdersPageModel>(Button.CommandProperty, v => v.LocalErrorCommand);

			var button4 = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Remote Error Placeholder Example",
			};
            button4.SetBinding<PlaceholdersPageModel>(Button.CommandProperty, v => v.RemoteErrorCommand);

			var imagePath = new Label() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				FontSize = 9,
			};
            imagePath.SetBinding<PlaceholdersPageModel>(Label.TextProperty, v => v.ImagePath);

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

            this.GetPageModel()
                .FreeResources();
        }
	}
}


