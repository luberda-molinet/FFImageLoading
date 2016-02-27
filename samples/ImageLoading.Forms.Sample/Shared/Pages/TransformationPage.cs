using System;
using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.PageModels;

namespace FFImageLoading.Forms.Sample.Pages
{
    public class TransformationPage : ContentPage, IBasePage<TransformationPageModel>
	{
		public TransformationPage()
		{
			Title = "Transformations Demo";

			var cachedImage = new CachedImage() {
				WidthRequest = 400,
				HeightRequest = 400,
				DownsampleToViewSize = true,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				CacheDuration = TimeSpan.FromDays(30),
			};

            cachedImage.SetBinding<TransformationPageModel>(CachedImage.TransformationsProperty, v => v.Transformations);
            cachedImage.SetBinding<TransformationPageModel>(CachedImage.LoadingPlaceholderProperty, v => v.LoadingImagePath);
            cachedImage.SetBinding<TransformationPageModel>(CachedImage.ErrorPlaceholderProperty, v => v.ErrorImagePath);
            cachedImage.SetBinding<TransformationPageModel>(CachedImage.SourceProperty, v => v.ImagePath);

			var reloadButton = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Load another image",
			};
            reloadButton.SetBinding<TransformationPageModel>(Button.CommandProperty, v => v.LoadAnotherImageCommand);

			var imagePath = new Label() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				VerticalOptions = LayoutOptions.FillAndExpand,
                HorizontalTextAlignment = TextAlignment.Center,
				FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label))
			};
            imagePath.SetBinding<TransformationPageModel>(Label.TextProperty, v => v.ImagePath);

			var transformatioType = new Label() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalTextAlignment = TextAlignment.Center,
			};
            transformatioType.SetBinding<TransformationPageModel>(Label.TextProperty, v => v.TransformationType);

			Content = new ScrollView() {
				Content = new StackLayout { 
					Children = {
						cachedImage,
						transformatioType,
						imagePath,
						reloadButton,
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


