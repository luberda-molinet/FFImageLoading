using System;

using Xamarin.Forms;
using DLToolkit.PageFactory;
using FFImageLoading.Forms.Sample.ViewModels;

namespace FFImageLoading.Forms.Sample.Pages
{
	public class TransformationExamplePage : PFContentPage<TransformationExampleViewModel>
	{
		public TransformationExamplePage()
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

			cachedImage.SetBinding<TransformationExampleViewModel>(CachedImage.TransformationsProperty, v => v.Transformations);
			cachedImage.SetBinding<TransformationExampleViewModel>(CachedImage.LoadingPlaceholderProperty, v => v.LoadingImagePath);
			cachedImage.SetBinding<TransformationExampleViewModel>(CachedImage.ErrorPlaceholderProperty, v => v.ErrorImagePath);
			cachedImage.SetBinding<TransformationExampleViewModel>(CachedImage.SourceProperty, v => v.ImagePath);

			var reloadButton = new Button() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				Text = "Load another image",
				Command = ViewModel.LoadAnotherImageCommand
			};

			var imagePath = new Label() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				VerticalOptions = LayoutOptions.FillAndExpand,
				XAlign = TextAlignment.Center,
				FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label))
			};
			imagePath.SetBinding<TransformationExampleViewModel>(Label.TextProperty, v => v.ImagePath);

			var transformatioType = new Label() {
				HorizontalOptions = LayoutOptions.FillAndExpand,
				XAlign = TextAlignment.Center,
			};
			transformatioType.SetBinding<TransformationExampleViewModel>(Label.TextProperty, v => v.TransformationType);

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
			ViewModel.ImagePath = null;
		}
	}
}


