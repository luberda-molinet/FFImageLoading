using System;
using Xamarin.Forms;
using FFImageLoading.Forms.Sample.PageModels;
using DLToolkit.PageFactory;

namespace FFImageLoading.Forms.Sample.Pages
{
    public class CropTransformationPage : ContentPage, IBasePage<CropTransformationPageModel>
	{
		public CropTransformationPage()
		{
			Title = "CropTransformation Demo";

			var cachedImage = new CachedImage() {
				WidthRequest = 300f,
				HeightRequest = 300f,
				DownsampleToViewSize = true,
				HorizontalOptions = LayoutOptions.FillAndExpand,
				VerticalOptions = LayoutOptions.FillAndExpand,
				CacheDuration = TimeSpan.FromDays(30),
				FadeAnimationEnabled = false,
			};

			cachedImage.SetBinding<CropTransformationPageModel>(CachedImage.TransformationsProperty, v => v.Transformations);
			cachedImage.SetBinding<CropTransformationPageModel>(CachedImage.LoadingPlaceholderProperty, v => v.LoadingImagePath);
			cachedImage.SetBinding<CropTransformationPageModel>(CachedImage.ErrorPlaceholderProperty, v => v.ErrorImagePath);
			cachedImage.SetBinding<CropTransformationPageModel>(CachedImage.SourceProperty, v => v.ImagePath);

			var imagePath = new Label() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
                HorizontalTextAlignment = TextAlignment.Center,
				FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label))
			};
            imagePath.SetBinding<CropTransformationPageModel>(Label.TextProperty, v => v.ImagePath);

			var cropAddXButton = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "X+",
			};
            cropAddXButton.SetBinding<CropTransformationPageModel>(Button.CommandProperty, v => v.AddCurrentXOffsetCommad);

			var cropSubXButton = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "X-",
			};
            cropSubXButton.SetBinding<CropTransformationPageModel>(Button.CommandProperty, v => v.SubCurrentXOffsetCommad);

			var cropAddYButton = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Y+",
			};
            cropAddYButton.SetBinding<CropTransformationPageModel>(Button.CommandProperty, v => v.AddCurrentYOffsetCommad);

			var cropSubYButton = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Y-",
			};
            cropSubYButton.SetBinding<CropTransformationPageModel>(Button.CommandProperty, v => v.SubCurrentYOffsetCommad);

			var cropAddZoomButton = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "+",
			};
            cropAddZoomButton.SetBinding<CropTransformationPageModel>(Button.CommandProperty, v => v.AddCurrentZoomFactorCommad);

			var cropSubZoomButton = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "-",
			};
            cropSubZoomButton.SetBinding<CropTransformationPageModel>(Button.CommandProperty, v => v.SubCurrentZoomFactorCommad);

			var buttonsLayout1 = new StackLayout() {
				Orientation = StackOrientation.Horizontal,
				Children = {
					cropAddXButton, 
					cropSubXButton,
					cropAddYButton,
					cropSubYButton,
				}
			};

			var buttonsLayout2 = new StackLayout() {
				Orientation = StackOrientation.Horizontal,
				Children = {
					cropAddZoomButton,
					cropSubZoomButton
				}
			};

			Content = new ScrollView() {
				Content = new StackLayout { 
					Children = {
						imagePath,
						cachedImage,
						buttonsLayout1,
						buttonsLayout2,
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


